using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Projections.Common.PTypes;
using Projections.Core.Data;
using Projections.Core.Maths;
using Projections.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace Projections.Content.Projectiles
{
    public class AllenKey : ModProjectile
    {
        private bool _initalized;
        private float _scale = 0;
        private const int DEF_WIDTH = 12;
        private const int DEF_HEIGHT = 24;
        private float _gravityAcc = 0;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            ProjectileID.Sets.TrailingMode[Type] = 0;
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.penetrate = 4;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Throwing;          
        }

        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
            _initalized = false;
        }

        const float PER_TICK = (1.0f / 60.0f);
        const float GRAVITY_ACC_SPEED = PER_TICK * 32.5f;
        const float TERMINAL_ACC = 64.0f;
        const float TERMINAL_Y = 12.0f;
        const float ROTATION_SPEED_MAX = ((float)Math.PI * 2.0f * 0.32f);
        const float MAX_X_VELO = 15.0f;
        private int _tileCollideTimer = 10;
        private PRarity _rarity;

        public override void AI()
        {
            if (!_initalized)
            {
                double rngVal = Main.rand.NextDouble();
                _scale = (float)Utils.Lerp(0.85, 1.75, rngVal);

                _rarity = (PRarity)(Main.rand.Next(0, (int)PRarity.__Count));

                float angle = (float)Utils.Lerp(-25f, -80f, Main.rand.NextDouble()) * PMath.DEG_2_RAD;

                Projectile.width = (int)(DEF_WIDTH * _scale);
                Projectile.height = (int)(DEF_WIDTH * _scale);
                Projectile.timeLeft = (int)(120 * _scale);
                Projectile.scale = _scale;
                Projectile.damage = (int)(Projectile.damage * _scale);

                float sign = Math.Sign(Projectile.velocity.X);
                Projectile.velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * PMath.Lerp(5.0f, 7.0f, (float)rngVal);

                Projectile.velocity.Y *= 1.65f;
                Projectile.velocity.X *= sign;
                Projectile.position.Y -= DEF_WIDTH * 0.15f;
                Projectile.rotation = Main.rand.NextFloat() * (float)Math.PI * 2.0f;

                _tileCollideTimer = 10;
                Projectile.tileCollide = _tileCollideTimer <= 0;

                _gravityAcc = 0;
                _initalized = true;
            }

            if(_immuneTimer > 0)
            {
                _immuneTimer--;
            }

            if(_tileCollideTimer > 0)
            {
                _tileCollideTimer--;
                Projectile.tileCollide = _tileCollideTimer <= 0;
            }

            float rotationDir;
            Projectile.velocity.Y += Math.Min(_gravityAcc, TERMINAL_ACC) * PER_TICK;
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y, TERMINAL_Y);
            if (Projectile.velocity.Y > 0)
            {
                rotationDir = PMath.InverseLerp(0.0f, 16.0f * PER_TICK, Projectile.velocity.Y) * 2.0f;
            }
            else
            {
                rotationDir = PMath.InverseLerp(0.0f, 10.0f * PER_TICK, -Projectile.velocity.Y);
            }

            rotationDir *= rotationDir * rotationDir;
            rotationDir *= Math.Abs(Projectile.velocity.X);

            if (Math.Abs(Projectile.velocity.X) > MAX_X_VELO)
            {
                Projectile.velocity.X -= Math.Sign(Projectile.velocity.X) * PER_TICK * (MAX_X_VELO * 0.25f);
            }

            Projectile.rotation += ROTATION_SPEED_MAX * rotationDir * PER_TICK;
            _gravityAcc += GRAVITY_ACC_SPEED;

            float t = (1.0f - PMath.InverseLerp(0, 60, Projectile.timeLeft));
            t *= t;
            Projectile.alpha = (int)(t * 255.0f);
            Lighting.AddLight(Projectile.Center, (_rarity.ToColor().ToVector3() * (Main.essScale * t)));
        }

        public override bool? CanHitNPC(NPC target)
        {
            return !target.friendly && _immuneTimer <= 0;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            bool hitCeiling = Projectile.velocity.Y <= 0;
            OnBounce(!hitCeiling, 0.45f, 1);
            if(Math.Abs(Projectile.velocity.Y) <= PER_TICK * 3.0f)
            {
                _gravityAcc = 0;
                Projectile.velocity.Y = 0;
            }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);
            OnBounce(true, 0.85f, 10);
        }

        private void OnBounce(bool flip, float reduce, int immuneTime)
        {
            Projectile.velocity.Y = flip ? -Projectile.velocity.Y * reduce : 0;
            _gravityAcc = 0;
            Projectile.velocity.X *= 0.45f;
            _immuneTimer = Math.Max(immuneTime, _immuneTimer);

            _tileCollideTimer = 1;
            Projectile.tileCollide = _tileCollideTimer <= 0;
        }

        private int _immuneTimer = 0;

        public override bool PreDraw(ref Color lightColor)
        {
            Asset<Texture2D> tex = ModContent.Request<Texture2D>(Texture);
            if(tex?.Value != null)
            {
                var col = _rarity.ToColor();
                int alpha = 255 - Projectile.alpha;
                lightColor.R = PMath.MultUI8LUT(lightColor.R, alpha);
                lightColor.G = PMath.MultUI8LUT(lightColor.G, alpha);
                lightColor.B = PMath.MultUI8LUT(lightColor.B, alpha);
                lightColor.A = PMath.MultUI8LUT(lightColor.A, alpha);

                col.R = PMath.MultUI8LUT(col.R, alpha);
                col.G = PMath.MultUI8LUT(col.G, alpha);
                col.B = PMath.MultUI8LUT(col.B, alpha);
                col.A = PMath.MultUI8LUT(col.A, alpha);

                var glow = ModContent.Request<Texture2D>("Projections/Content/Projectiles/AllenKey_Glow").Value;

                float vel = PMath.InverseLerp(0.0f, 6.0f * PER_TICK, Projectile.velocity.Length()) * 0.5f;
                for (int k = 0; k < Projectile.oldPos.Length; k++)
                {
                    Color color = col * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length) * (k > 0 ? vel : 1.0f);
                    ProjectionUtils.DrawInWorld(Main.spriteBatch, Projectile.oldPos[k], Projectile.width, Projectile.height, DEF_HEIGHT * _scale, glow, color, 1.0f, null, Projectile.rotation);
                }
                ProjectionUtils.DrawInWorld(Main.spriteBatch, Projectile.position, Projectile.width, Projectile.height, DEF_HEIGHT * _scale, tex.Value, lightColor, 1.0f, null, Projectile.rotation);
            }
            return false;
        }
    }
}
