using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using SharpDX;
using Color = System.Drawing.Color;

namespace GloriousRevolution
{
    public class Program
    {
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;
        private static Spell Q, W, E, R;
        private const string Champ = "Viktor";
        private static Orbwalking.Orbwalker _orbwalker;
        private static Menu _firstMenu;
        private const int MaxRangeE = 1225;
        private const int LengthE = 700;
        private const int ERange = 525;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Viktor")
                return;

            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, 525);
            R = new Spell(SpellSlot.R, 700);

        

            Q.SetTargetted(0.25f, 2000);
            W.SetSkillshot(0.25f, 300, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.0f, 90, 1200, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.25f, 250, float.MaxValue, false, SkillshotType.SkillshotCircle);

            _firstMenu = new Menu("GloriousEvolution" + Champ, "firstMenu", true);

            Menu orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            _firstMenu.AddSubMenu(orbwalkerMenu);

            Menu ts = _firstMenu.AddSubMenu(new Menu("Target Selector", "Target Selector")); ;
            TargetSelector.AddToMenu(ts);

            Menu combo = _firstMenu.AddSubMenu(new Menu("Combo", "Combo"));
            combo.AddItem(new MenuItem("QC", "Q").SetValue(true));
            combo.AddItem(new MenuItem("WC", "W").SetValue(true));
            combo.AddItem(new MenuItem("EC", "E").SetValue(true));
            combo.AddItem(new MenuItem("RC", "R").SetValue(true));
            combo.AddItem(new MenuItem("RMC", "use R on x enemies").SetValue(new Slider(2, 1, 5)));

            Menu harass = _firstMenu.AddSubMenu(new Menu("Harass", "Harass"));
            harass.AddItem(new MenuItem("QH", "Q").SetValue(true));
            harass.AddItem(new MenuItem("EH", "E").SetValue(true));
            harass.AddItem(new MenuItem("ManaH", "Min mana harass").SetValue(new Slider(40, 0, 100)));

            Menu extra = _firstMenu.AddSubMenu(new Menu("Extra", "Extra"));
            extra.AddItem(new MenuItem("AGCW", "Gapclose W").SetValue(true));
            extra.AddItem(new MenuItem("IR", "Interrupt W").SetValue(true));
            extra.AddItem(new MenuItem("CCW", "Chain stun W").SetValue(true));

            Menu draw = _firstMenu.AddSubMenu(new Menu("Drawing", "Drawing"));
            draw.AddItem(new MenuItem("DLH", "Draw Last Hit").SetValue(true));
            draw.AddItem(new MenuItem("DK", "Draw Killable").SetValue(true));

            _firstMenu.AddToMainMenu();

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_Ondraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;

        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (_firstMenu.Item("AGCW").GetValue<bool>())
          
                if (gapcloser.End.Distance(ObjectManager.Player.ServerPosition) < W.Range)
                {
                    W.Cast(gapcloser.End);
                }
        }


        private static void Game_OnUpdate(EventArgs args)
        {
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo();
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                Harass();
        }

        private static void Combo()
        {

            if (E.IsReady() && _firstMenu.Item("EC").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(MaxRangeE, TargetSelector.DamageType.Magical);

                if (target != null)
                    PredictE(target);
            }

            if (Q.IsReady() && _firstMenu.Item("QC").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

                if (target != null)
                    Q.Cast(target);
            }

            if (W.IsReady() && _firstMenu.Item("WC").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
                var predictionW = W.GetPrediction(target);

                if (target != null && predictionW.Hitchance >= HitChance.VeryHigh)
                    W.Cast(target.ServerPosition);
               
            }

            if (R.IsReady() && _firstMenu.Item("RC").GetValue<bool>() && IsKillable())
            {
                var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
                if (_firstMenu.Item("RMC").GetValue<int>() == 1)
                {
                    if (target != null && target.HealthPercent > 15 || Player.HealthPercent < 25 || target.Level >= Player.Level + 2)
                        R.Cast(target);

                } else {

                    R.CastIfWillHit(target, _firstMenu.Item("RMC").GetValue<int>());
                }
            }

        }

        private static void Harass()
        {
            if (Player.ManaPercent >= _firstMenu.Item("ManaH").GetValue<Slider>().Value)
            {
                if (E.IsReady() && _firstMenu.Item("EH").GetValue<bool>())
                {
                    var target = TargetSelector.GetTarget(MaxRangeE, TargetSelector.DamageType.Magical);

                    if (target != null)
                        PredictE(target);
                }

                if (Q.IsReady() && _firstMenu.Item("QH").GetValue<bool>())
                {
                    var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

                    if (target != null)
                        Q.Cast(target);
                }
            }
        }

        private static void PredictE(Obj_AI_Hero target)
        {
            if (Player.ServerPosition.Distance(target.ServerPosition) < MaxRangeE-LengthE)
            {
                E.UpdateSourcePosition(target.ServerPosition, target.ServerPosition);
                var prediction =E.GetPrediction(target, true);
                if (target.IsValidTarget(E.Range) && prediction.Hitchance == HitChance.High)
                    E.Cast(target);

            }
            else if (Player.ServerPosition.Distance(target.ServerPosition) < E.Range + ERange)
            {
                var castStartPos = Player.ServerPosition.Extend(target.ServerPosition, ERange);
                E.UpdateSourcePosition(castStartPos, castStartPos);
                var prediction = E.GetPrediction(target, true);
                if (prediction.Hitchance >= HitChance.High)
                    E.Cast(castStartPos, prediction.CastPosition);
            }
        }


        private static void Drawing_Ondraw(EventArgs args)
        {
            if (Player.IsDead)
                return;

            if (_firstMenu.Item("DK").GetValue<bool>())
            {
                foreach (var enemy in HeroManager.Enemies)
                {
                    if (enemy.IsVisible && !enemy.IsDead && IsKillable())
                    {
                        Drawing.DrawText(enemy.HPBarPosition.X, enemy.HPBarPosition.Y + 50, Color.Red, "Killable");
                    }
                }
            }

            if (_firstMenu.Item("DK").GetValue<bool>())
            {
                foreach (Obj_AI_Minion minion in MinionManager.GetMinions(1500))
                {
                    if (!minion.IsDead && minion.IsVisible && !minion.IsAlly)
                    {
                        var damage = Player.GetAutoAttackDamage(minion, false);
                        if (damage >= minion.Health)
                        {
                            Render.Circle.DrawCircle(minion.Position, minion.BoundingRadius + 10, Color.Red);
                        }
                    }
                }
            }
        }



        public static bool IsKillable()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(2500, TargetSelector.DamageType.Magical);
            if (target != null)
            {
                double damage = 0d;
                var qaaDmg = new Double[] {20, 40, 60, 80, 100};
                if (_firstMenu.Item("QC").GetValue<bool>() && Q.IsReady())
                {
                    damage += target.GetSpellDamage(target, SpellSlot.Q);
                    damage += target.CalcDamage(target, Damage.DamageType.Magical, qaaDmg[Q.Level - 1] + 0.5 * target.TotalMagicalDamage + target.TotalAttackDamage);
                }

                if (_firstMenu.Item("QC").GetValue<bool>() && !Q.IsReady() && Player.HasBuff("viktorpowertransferreturn"))
                {
                    damage += Player.CalcDamage(target, Damage.DamageType.Magical, qaaDmg[Q.Level - 1] + 0.5 * Player.TotalMagicalDamage + Player.TotalAttackDamage);
                }

               
                if (_firstMenu.Item("EC").GetValue<bool>() && E.IsReady())
                {
                    if (Player.HasBuff("viktoreaug") || Player.HasBuff("viktorqeaug") || Player.HasBuff("viktorqweaug"))
                        damage += Player.GetSpellDamage(target, SpellSlot.E, 1);
                    else
                        damage += Player.GetSpellDamage(target, SpellSlot.E);
                }

              
                if (_firstMenu.Item("RC").GetValue<bool>() && R.IsReady())
                {
                    damage += Damage.GetSpellDamage(Player, target, SpellSlot.R, 1);
                    damage += Damage.GetSpellDamage(Player, target, SpellSlot.R);
                }

                if (damage > target.Health)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
