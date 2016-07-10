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
        private static Orbwalking.Orbwalker orbwalker;
        private static Menu firstMenu;


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

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_Ondraw;

        }



        private static void Game_OnUpdate(EventArgs args)
        {
            throw new NotImplementedException();
        }

        private static void Drawing_Ondraw(EventArgs args)
        {
            foreach (Obj_AI_Hero enemy in HeroManager.Enemies)
            {
                if (enemy.IsVisible && !enemy.IsDead && IsKillable())
                {
                    Drawing.DrawText(enemy.HPBarPosition.X, enemy.HPBarPosition.Y + 50, Color.Red, "Killable");
                }
            }

            foreach (Obj_AI_Minion minion in MinionManager.GetMinions(1500))
            {
                if (!minion.IsDead && minion.IsVisible && !minion.IsAlly)
                {
                    double damage = Damage.GetAutoAttackDamage(Player, minion, false);
                    if (damage > minion.Health)
                    {
                        Render.Circle.DrawCircle(minion.Position, minion.BoundingRadius + 10, Color.Red);
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
