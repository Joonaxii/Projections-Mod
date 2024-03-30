using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Projections.Content.Items;
using Projections.Content.Items.Consumables;
using Projections.Content.Items.Projectors;
using Projections.Content.Items.RarityStones;
using Projections.Content.Projectiles;
using Projections.Core.Data.Structures;
using Projections.Core.Systems;
using Projections.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Projections.Content.NPCs
{
    [AutoloadHead]
    public class ProjectionTrader : ModNPC
    {
        public override string Texture => "Projections/Content/NPCs/ProjectionTrader";

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 26;
            NPCID.Sets.ExtraFramesCount[NPC.type] = 9;
            NPCID.Sets.AttackFrameCount[NPC.type] = 4;
            NPCID.Sets.DangerDetectRange[NPC.type] = 1200;
            NPCID.Sets.AttackType[NPC.type] = 0;
            NPCID.Sets.AttackTime[NPC.type] = 1;
            NPCID.Sets.AttackAverageChance[NPC.type] = 3;
            NPCID.Sets.HatOffsetY[NPC.type] = 4;
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = 7;
            NPC.damage = 4;
            NPC.defense = 6666;
            NPC.lifeMax = 6666;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = new Terraria.Audio.SoundStyle("Projections/Content/Audio/NPC/tim");
            NPC.knockBackResist = 0.05f;
            AnimationType = NPCID.TravellingMerchant;
        }

        public override bool CanTownNPCSpawn(int numTownNPCs)
        {
            if (ProjectorSystem.TotalProjectorCount > 0) { return true; }
            int id0 = ModContent.ItemType<ProjectionItem>();
            int id1 = ModContent.ItemType<ProjectionMaterial>();
            for (int k = 0; k < 255; k++)
            {
                Player player = Main.player[k];
                if (!player.active)
                {
                    continue;
                }

                if (player.HasItemInAnyInventory(id0)) { return true; }
                if (player.HasItemInAnyInventory(id1)) { return true; }
            }
            return false;
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("LegacyInterface.28");
            var pPlayer = Main.LocalPlayer.PPlayer();
            button2 = "Status";
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (!firstButton)
            {
                Main.npcChatText = GetStatus();
                return;
            }
            shopName = "Projections";
        }

        public override void AddShops()
        {
            var npcShop = new NPCShop(Type, "Projections")
                //Empty Projection Frame
                .Add<ProjectionItem>()
                //Rarity Stones
                .Add<RarityStone0>(Condition.DownedEarlygameBoss)
                .Add<RarityStone1>(Condition.DownedSkeletron)
                .Add<RarityStone2>(Condition.Hardmode)
                .Add<RarityStone3>(Condition.DownedMechBossAny)
                .Add<RarityStone4>(Condition.DownedCultist);
            npcShop.Register();
        }

        public override void ModifyActiveShop(string shopName, Item[] items)
        {
            if (Main.netMode == NetmodeID.Server) { return; }

            var pPlr = Main.LocalPlayer.PPlayer();
            if (pPlr == null) { return; }

            Span<ProjectionIndex> poolPT = stackalloc ProjectionIndex[10]
            {
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
            };
            Span<ProjectionIndex> poolMT = stackalloc ProjectionIndex[10]
            {
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
            };

            pPlr.FetchPools(poolPT, poolMT, out var poolP, out var poolM);
            for (int i = 10, j = 20, k = 0, l = 0; i < 20; i++, j++)
            {
                ref var iP = ref items[i];
                ref var iM = ref items[j];
                if (iP == null && k < poolP.Length && poolP[k].IsValidID())
                {
                    iP = ProjectionUtils.NewProjectionItem<ProjectionItem>(poolP[k++], ProjectionSource.Shop)?.Item;
                }

                if (iM == null && l < poolM.Length && poolM[k].IsValidID())
                {
                    iM = ProjectionUtils.NewProjectionItem<ProjectionMaterial>(poolM[l++], ProjectionSource.Shop)?.Item;
                }
            }
        }

        public override bool CheckConditions(int left, int right, int top, int bottom)
        {
            if (ProjectorSystem.Instance.TileProjectors.Count > 0 ||
            ProjectorSystem.Instance.CustomProjectors.Count > 0)
            {
                return true;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                var pPlayer = Main.player[i].PPlayer();
                if (pPlayer != null && pPlayer.Player.active)
                {
                    if (pPlayer.CanProject || pPlayer.CollectedProjections.Count > 0) { return true; }
                }
            }

            return true;
        }

        public override List<string> SetNPCNameList()
        {
            return new List<string>() { "Tim", "Allen" };
        }

        public override string GetChat()
        {
            int randVal = Main.rand.Next(10);
            switch (randVal)
            {
                case 1:
                    return "Your Home could use some Improvement.";
                case 0:
                    SoundEngine.PlaySound(new SoundStyle("Projections/Content/Audio/NPC/tim"), NPC.Center);
                    return "Augh!?";
                case 2:
                    int nearby = NPC.Center.CountNearByProjectors(48, -1, requireActive: true);

                    if (nearby >= 32)
                    {
                        SoundEngine.PlaySound(new SoundStyle("Projections/Content/Audio/NPC/tim"), NPC.Center);
                        return $"Augh!? That's AN EXCESSIVE nubmer of projectors nearby!! ({nearby} nearby)";
                    }
                    else if(nearby >= 16)
                    {
                        return $"Whoa! That's A LOT of projectors!! ({nearby} nearby)";
                    }
                    else if (nearby >= 8)
                    {
                        return $"I really like that we have projectors around. ({nearby} nearby)";
                    }
                    else if (nearby >= 1)
                    {
                        return $"There could be few more projectors around... ({nearby} nearby)";
                    }
                    return "*sigh* Not a single projector in sight...";
                default:
                    randVal = Main.rand.Next(5);
                    var pPlayer = Main.LocalPlayer.PPlayer();
                    switch (randVal)
                    {
                        default:
                            return "I wish more people did stuff with projections. What?!, I'm not projecting my potentially bad choice of career!";
                        case 1:
                            return pPlayer?.HasBookOfProjections ?? false ? "Look who decided to show up, the projection collector themself!" : $"I'd suggest you get yourself a [item/s1:{ModContent.ItemType<BookOfProjections>()}], it should help you with your projection collection efforts.";
                        case 2:
                            return pPlayer?.CanProject ?? false ? "Have you been projecting a lot lately?" : $"I heard that you could get yourself a [item/s1:{ModContent.ItemType<LicenseToProject>()}] by throwing a [item/s1:{ModContent.ItemType<BookOfProjections>()}] in some sort of magical liquid. Could be worth to check it out!";
                    }
            }
        }

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            damage = 4;
            knockback = 1;
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 5;
            randExtraCooldown = 1;
        }

        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            projType = ModContent.ProjectileType<AllenKey>();
            attackDelay = 1;
        }

        public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
        {
            multiplier = 5.25f;
            randomOffset = 0.25f;
        }

        private string GetStatus()
        {
            var randVal = Main.rand.Next(2);
            var pPlayer = Main.LocalPlayer.PPlayer();
            switch (randVal)
            {
                case -2:
                    return $"Nothing much to say here, you really ought to get yourself a [item/s1:{ModContent.ItemType<LicenseToProject>()}], before then I can't really do much.";
                case -1:
                    return (pPlayer?.HasBookOfProjections ?? false) ?
                        $"Nothing much to say here, though you really ought to get yourself a [item/s1:{ModContent.ItemType<BookOfProjections>()}], that should help you find/create projections." :
                        "What are you waiting for, go collect some projections!";
                case 0:
                    {
                        int projCount = pPlayer?.CollectedProjections.Count ?? 0;
                        if (projCount > 0)
                        {
                            if (projCount >= 500)
                            {
                                SoundEngine.PlaySound(new SoundStyle("Projections/Content/Audio/NPC/tim"), NPC.Center);
                                return $"AUGH?! Your projection collection rivals my own! (Collected: {projCount})";
                            }
                            else if (projCount >= 100)
                            {
                                return $"Your projection collecting process seems to be going well! (Collected: {projCount})";
                            }
                            else if (projCount >= 25)
                            {
                                return $"Keep bringing those numbers up! (Collected: {projCount})";
                            }
                            else if (projCount >= 2)
                            {
                                return $"Everyone starts their collecting somewhere. (Collected: {projCount})";
                            }
                            else if (projCount >= 1)
                            {
                                return $"Your very first projection is now collected! (Collected: {projCount})";
                            }
                        }
                    }
                    goto case -1;
                case 1:
                    if (pPlayer?.CanProject ?? false)
                    {
                        var total = pPlayer.TimeProjected;
                        if (total.TotalDays >= 1)
                        {
                            SoundEngine.PlaySound(new SoundStyle("Projections/Content/Audio/NPC/tim"), NPC.Center);
                            return $"AUGH?! You've been projecting for a grand total of {total.ToDurationString()}! That's impressive.";
                        }
                        else if (total.TotalHours >= 12)
                        {
                            return $"Wow, half a day of projecting! (Time Projected: {total.ToDurationString()})";
                        }
                        else if (total.TotalHours >= 1)
                        {
                            return $"You're getting used to projecting aren't you. (Time Projected: {total.ToDurationString()})";
                        }
                        else if (total.Minutes >= 30)
                        {
                            return $"Nice, around half an hour of projecting! (Time Projected: {total.ToDurationString()})";
                        }
                        else if (total.Minutes >= 1)
                        {
                            return $"Everything starts somewhere. (Time Projected: {total.ToDurationString()})";
                        }
                        else if (total.Seconds >= 30)
                        {
                            return $"You'll get the hang of it... eventually. (Time Projected: {total.ToDurationString()})";
                        }
                        else if (total.Seconds >= 1)
                        {
                            return $"You've barely projected at all! (Time Projected: {total.ToDurationString()})";
                        }
                        return $"There's barely any time on your projecting record! (Time Projected: {total.ToDurationString()})";

                    }
                    goto case -2;
            }
            return "Yean nothing much to say here, go collect some projections!";
        }
    }
}
