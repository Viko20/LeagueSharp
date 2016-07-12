using System;
using System.Drawing;
using System.Net;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;

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

            var qspell = SpellDatabase.GetBySpellSlot(SpellSlot.Q, Champ);
            var wspell = SpellDatabase.GetBySpellSlot(SpellSlot.W, Champ);
            var espell = SpellDatabase.GetBySpellSlot(SpellSlot.E, Champ);
            var rspell = SpellDatabase.GetBySpellSlot(SpellSlot.R, Champ);

            Q.SetTargetted(qspell.Delay, qspell.MissileSpeed);
            W.SetSkillshot(wspell.Delay, wspell.Width, wspell.MissileSpeed, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(espell.Delay, espell.Width, espell.MissileSpeed, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(rspell.Delay, rspell.Width, rspell.MissileSpeed, false, SkillshotType.SkillshotCircle);

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
            extra.AddItem(new MenuItem("IR", "Interrupt R").SetValue(true));
            extra.AddItem(new MenuItem("CCW", "Chain stun W").SetValue(true));

            Menu draw = _firstMenu.AddSubMenu(new Menu("Drawing", "Drawing"));
            draw.AddItem(new MenuItem("DLH", "Draw Last Hit").SetValue(true));
            draw.AddItem(new MenuItem("DK", "Draw Killable").SetValue(true));

            _firstMenu.AddToMainMenu();

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_Ondraw;

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
                    E.Cast(target);
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

                if (target != null)
                    W.Cast(target);
                
            }

            if (R.IsReady() && _firstMenu.Item("RC").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);

                if (target != null)
                    R.Cast(target);

            }

        }

        private static void Harass()
        {
            if (Player.ManaPercent >= _firstMenu.Item("ManaH").GetValue<Slider>().Value)
            {
                if (E.IsReady() && _firstMenu.Item("EC").GetValue<bool>())
                {
                    var target = TargetSelector.GetTarget(MaxRangeE, TargetSelector.DamageType.Magical);

                    if (target != null)
                        E.Cast(target);
                }

                if (Q.IsReady() && _firstMenu.Item("QC").GetValue<bool>())
                {
                    var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

                    if (target != null)
                        Q.Cast(target);
                }
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
                        if (damage > minion.Health)
                        {
                            Render.Circle.DrawCircle(minion.Position, minion.BoundingRadius + 10, Color.Red);
                        }
                    }
                }
            }
        }



        public static bool IsKillable()
        {
            Obj_AI_Hero comboTarget = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Magical);
            if (comboTarget != null)
            {
                double damage = 0d;
                damage = Player.GetSpellDamage(comboTarget, SpellSlot.Q) +
                         Player.GetSpellDamage(comboTarget, SpellSlot.E) +
                         Player.GetSpellDamage(comboTarget, SpellSlot.R);

                if (damage > comboTarget.Health)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
