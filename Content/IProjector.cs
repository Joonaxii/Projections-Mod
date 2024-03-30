using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Projections.Core.Data;
using ReLogic.Content;

namespace Projections.Content
{
    public interface IProjector
    {
        string NameExtension { get; }

        ProjectorType ProjectorType { get; }
        int SlotCount { get; }
        uint CreatorTag { get; }
        Vector2 Hotspot { get; }
        bool CanBeUsedInRecipe { get; }

        Asset<Texture2D> GetMainTexture();
        Asset<Texture2D> GetGlowTexture();

        Rectangle? GetUVs(bool main, bool isActive);
    }
}