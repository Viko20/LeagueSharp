using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace GloriousRevolution
{
    public class Program
    {
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;
        private static Spell _q, _w, _e, _r;
        private const string Champ = "Viktor";
        private static Orbwalking.Orbwalker _orbwalker;
        private static Menu _firstMenu;
        private const int MaxRangeE = 1225;
        private const int LengthE = 700;
        private const int ERange = 525;

        private static void Main()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Viktor")
                return;

            _q = new Spell(SpellSlot.Q, 600);
            _w = new Spell(SpellSlot.W, 700);
            _e = new Spell(SpellSlot.E, 525);
            _r = new Spell(SpellSlot.R, 700);


            _q.SetTargetted(0.25f, 2000);
            _w.SetSkillshot(0.25f, 300, float.MaxValue, false, SkillshotType.SkillshotCircle);
            _e.SetSkillshot(0.0f, 90, 1200, false, SkillshotType.SkillshotLine);
            _r.SetSkillshot(0.25f, 250, float.MaxValue, false, SkillshotType.SkillshotCircle);

            _firstMenu = new Menu("GloriousEvolution" + Champ, "firstMenu", true);

            var orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            _firstMenu.AddSubMenu(orbwalkerMenu);

            var combo = _firstMenu.AddSubMenu(new Menu("Combo", "Combo"));
            combo.AddItem(new MenuItem("QC", "Q").SetValue(true));
            combo.AddItem(new MenuItem("WC", "W").SetValue(true));
            combo.AddItem(new MenuItem("EC", "E").SetValue(true));
            combo.AddItem(new MenuItem("RC", "R").SetValue(true));
            combo.AddItem(new MenuItem("RMC", "use R on x enemies").SetValue(new Slider(2, 1, 5)));
            combo.AddItem(new MenuItem("WMC", "use W on x enemies").SetValue(new Slider(2, 1, 5)));

            var harass = _firstMenu.AddSubMenu(new Menu("Harass", "Harass"));
            harass.AddItem(new MenuItem("QH", "Q").SetValue(true));
            harass.AddItem(new MenuItem("EH", "E").SetValue(true));
            harass.AddItem(new MenuItem("ManaH", "Min mana harass").SetValue(new Slider(40)));

            var extra = _firstMenu.AddSubMenu(new Menu("Extra", "Extra"));
            extra.AddItem(new MenuItem("AGCW", "Gapclose W").SetValue(true));
            extra.AddItem(new MenuItem("IR", "Interrupt W").SetValue(true));
            extra.AddItem(new MenuItem("CCW", "Chain cc W").SetValue(true));

            var draw = _firstMenu.AddSubMenu(new Menu("Drawing", "Drawing"));
            draw.AddItem(new MenuItem("DK", "Draw Killable").SetValue(true));
            draw.AddItem(new MenuItem("FA", "Fed Alert").SetValue(true));

            _firstMenu.AddToMainMenu();

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_Ondraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
        }

        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender,
            Interrupter2.InterruptableTargetEventArgs args)
        {
            if (_w.IsReady() && sender.IsValidTarget(_w.Range) && !sender.IsZombie &&
                _firstMenu.Item("IR").GetValue<bool>())
            {
                _w.Cast(sender);
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (_firstMenu.Item("AGCW").GetValue<bool>())

                if (gapcloser.End.Distance(ObjectManager.Player.ServerPosition) < _w.Range)
                {
                    _w.Cast(gapcloser.End);
                }
        }


        private static void Game_OnUpdate(EventArgs args)
        {
            if (Player.IsDead || Player.IsRecalling())
                return;

            UltimateFollow();
            ChainW();

            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo();
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                Harass();       
        }

        private static void Combo()
        {
            if (_e.IsReady() && _firstMenu.Item("EC").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(MaxRangeE, TargetSelector.DamageType.Magical);

                if (target != null)
                    PredictE(target);
            }

            if (_q.IsReady() && _firstMenu.Item("QC").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);

                if (target != null)
                    _q.Cast(target);
            }

            if (_w.IsReady() && _firstMenu.Item("WC").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Magical);
                var predictionW = _w.GetPrediction(target);

                if (target != null && predictionW.Hitchance >= HitChance.High) { 
                    foreach (var hero in HeroManager.Enemies.Where(x => x.IsValidTarget(_w.Range, false)))
                    {
                        if (hero.CountEnemiesInRange(200) >= _firstMenu.Item("WMC").GetValue<Slider>().Value)
                            _w.Cast(hero.ServerPosition);
                    }
                }
            }

            if (_r.IsReady() && _firstMenu.Item("RC").GetValue<bool>() && IsKillable())
            {
                var target = TargetSelector.GetTarget(_r.Range, TargetSelector.DamageType.Magical);
                if (_firstMenu.Item("RMC").GetValue<Slider>().Value == 1)
                {
                    if (target != null && target.HealthPercent > 15 || Player.HealthPercent < 25 ||
                        target.Level >= Player.Level + 2)
                        _r.Cast(target);
                }
                else
                {
                    foreach (var hero in HeroManager.Enemies.Where(x => x.IsValidTarget(_r.Range, false)))
                    {
                        if (hero.CountEnemiesInRange(325) >= _firstMenu.Item("RMC").GetValue<Slider>().Value)
                            _r.Cast(hero);
                    }
                }
            }
        }

        private static void UltimateFollow()
        {
            if (_r.Instance.Name != "ViktorChaosStorm")
            {
                var target = TargetSelector.GetTarget(Player, 1100, TargetSelector.DamageType.Magical);
                if (target != null)
                    Utility.DelayAction.Add(125, () => _r.Cast(target));
            }
        }

        private static void Harass()
        {
            if (Player.ManaPercent >= _firstMenu.Item("ManaH").GetValue<Slider>().Value)
            {
                if (_e.IsReady() && _firstMenu.Item("EH").GetValue<bool>())
                {
                    var target = TargetSelector.GetTarget(MaxRangeE, TargetSelector.DamageType.Magical);

                    if (target != null)
                        PredictE(target);
                }

                if (_q.IsReady() && _firstMenu.Item("QH").GetValue<bool>())
                {
                    var target = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);

                    if (target != null)
                        _q.Cast(target);
                }
            }
        }

        private static void ChainW()
        {
            if (_w.IsReady() && _firstMenu.Item("CCW").GetValue<bool>())
            {
                var targets = HeroManager.Enemies.Where(x => x.IsValidTarget(_w.Range));
                foreach (var enemy in targets)
                {
                    if (enemy.IsCharmed || enemy.IsMovementImpaired() || enemy.IsRooted || enemy.IsStunned ||
                        enemy.IsRecalling())
                    {
                        _w.Cast(enemy);
                    }
                }
            }
        }

        private static void PredictE(Obj_AI_Hero target)
        {
            if (Player.ServerPosition.Distance(target.ServerPosition) < MaxRangeE - LengthE)
            {
                _e.UpdateSourcePosition(target.ServerPosition, target.ServerPosition);
                var prediction = _e.GetPrediction(target, true);
                if (target.IsValidTarget(_e.Range) && prediction.Hitchance == HitChance.High)
                    _e.Cast(target);
            }
            else if (Player.ServerPosition.Distance(target.ServerPosition) < _e.Range + ERange)
            {
                var castStartPos = Player.ServerPosition.Extend(target.ServerPosition, ERange);
                _e.UpdateSourcePosition(castStartPos, castStartPos);
                var prediction = _e.GetPrediction(target, true);

                if (prediction.Hitchance >= HitChance.High)
                    _e.Cast(castStartPos, prediction.CastPosition);
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
                        Drawing.DrawText(enemy.HPBarPosition.X, enemy.HPBarPosition.Y + 50, Color.Green, "Killable");
                    }
                }
            }
            if (_firstMenu.Item("DK").GetValue<bool>())
            {
                foreach (var enemy in HeroManager.Enemies)
                {
                    if (enemy.IsVisible && !enemy.IsDead &&
                        enemy.ChampionsKilled + enemy.Assists > Player.Assists + Player.ChampionsKilled + 3)
                    {
                        Drawing.DrawText(enemy.HPBarPosition.X, enemy.HPBarPosition.Y + 100, Color.Red, "Om nom nom!");
                    }
                }
            }
        }


        public static bool IsKillable()
        {
            var target = TargetSelector.GetTarget(2500, TargetSelector.DamageType.Magical);
            if (target != null)
            {
                var damage = 0d;
                var qaaDmg = new double[] {20, 40, 60, 80, 100};
                if (_firstMenu.Item("QC").GetValue<bool>() && _q.IsReady())
                {
                    damage += target.GetSpellDamage(target, SpellSlot.Q);
                    damage += target.CalcDamage(target, Damage.DamageType.Magical,
                        qaaDmg[_q.Level - 1] + 0.5*target.TotalMagicalDamage + target.TotalAttackDamage);
                }

                if (_firstMenu.Item("QC").GetValue<bool>() && !_q.IsReady() &&
                    Player.HasBuff("viktorpowertransferreturn"))
                {
                    damage += Player.CalcDamage(target, Damage.DamageType.Magical,
                        qaaDmg[_q.Level - 1] + 0.5*Player.TotalMagicalDamage + Player.TotalAttackDamage);
                }


                if (_firstMenu.Item("EC").GetValue<bool>() && _e.IsReady())
                {
                    if (Player.HasBuff("viktoreaug") || Player.HasBuff("viktorqeaug") || Player.HasBuff("viktorqweaug"))
                        damage += Player.GetSpellDamage(target, SpellSlot.E, 1);
                    else
                        damage += Player.GetSpellDamage(target, SpellSlot.E);
                }


                if (_firstMenu.Item("RC").GetValue<bool>() && _r.IsReady())
                {
                    damage += Player.GetSpellDamage(target, SpellSlot.R, 1);
                    damage += Player.GetSpellDamage(target, SpellSlot.R);
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
