using Microsoft.Xna.Framework;
using Projections.Common.ProjectorTypes;
using Projections.Common.PTypes;
using Projections.Content;
using Projections.Core.Data;
using Projections.Core.Systems;
using Projections.Core.Utilities;
using System;
using Terraria.ModLoader;

namespace Projections
{
    public static class ProjectionsAPI
    {
        /// <summary>
        /// Registers an external <see cref="Projection"/> for loading.
        /// </summary>
        public static bool Register(Projection projection) => Projections.RegisterExternal(projection);

        /// <summary>
        /// Registers an external <see cref="PMaterial"/> for loading.
        /// </summary>
        public static bool Register(PMaterial material) => Projections.RegisterExternal(material);

        /// <summary>
        /// Unregisters an external <see cref="Projection"/>, also unloads it if it's loaded.
        /// </summary>
        public static bool Unregister(Projection projection) => Projections.UnregisterExternal(projection);

        /// <summary>
        /// Unregisters an external <see cref="PMaterial"/>, also unloads it if it's loaded.
        /// </summary>
        public static bool Unregister(PMaterial material) => Projections.UnregisterExternal(material);

        public static bool RegisterProjectorType(ReadOnlySpan<char> projectorTag, CreateProjector createMethod)
        {
            return ProjectorSystem.RegisterProjectorType(projectorTag, createMethod);
        }
        public static bool UnregisterProjectorType(ReadOnlySpan<char> projectorTag)
        {
            return ProjectorSystem.UnregisterProjectorType(projectorTag);
        }

        public static Projector NewProjector(ReadOnlySpan<char> creatorTag, int slotCount, Vector2 hotspot, Vector2 position = default)
        {
            TryGetCreatorTagID(creatorTag, out uint tagID);
            return NewProjector(tagID, slotCount, hotspot, position);
        }
        public static Projector NewProjector(uint creatorID, int slotCount, Vector2 hotspot, Vector2 position = default)
        {
            if(creatorID == 0)
            {
                Projections.Log(LogType.Error, "Could not create new projector, given CreatorID was 0!");
                return null;
            }
            ProjectorData pdata = ProjectorData.NewCustom(creatorID, hotspot, slotCount, position);
            return ProjectorSystem.GetNewProjector(in pdata);
        }

        public static Projector NewPlayerProjector(ReadOnlySpan<char> creatorTag, int player, int projectorSlot, int slotCount, Vector2 hotspot)
        {
            TryGetCreatorTagID(creatorTag, out uint tagID);
            return NewPlayerProjector(tagID, player, projectorSlot, slotCount, hotspot);
        }
        public static Projector NewPlayerProjector(uint creatorID, int player, int projectorSlot, int slotCount, Vector2 hotspot)
        {
            if (creatorID == 0)
            {
                Projections.Log(LogType.Error, "Could not create new projector, given CreatorID was 0!");
                return null;
            }
            ProjectorData pdata = ProjectorData.NewPlayer(creatorID, hotspot, slotCount, player, projectorSlot);
            return ProjectorSystem.GetNewProjector(in pdata);
        }

        public static Projector NewTileProjector(ReadOnlySpan<char> creatorTag, Point tilePosition, int slotCount, Point tileRegion, Vector2 hotspot, int style = 0)
        {
            TryGetCreatorTagID(creatorTag, out uint tagID);
            return NewTileProjector(tagID, tilePosition, slotCount, tileRegion, hotspot, style);
        }
        public static Projector NewTileProjector(uint creatorID, Point tilePosition, int slotCount, Point tileRegion, Vector2 hotspot, int style = 0)
        {
            if (creatorID == 0)
            {
                Projections.Log(LogType.Error, "Could not create new projector, given CreatorID was 0!");
                return null;
            }
            ProjectorData pdata = ProjectorData.NewTile(creatorID, hotspot, slotCount, tilePosition, tileRegion, style);
            return ProjectorSystem.GetNewProjector(in pdata);
        }

        public static void MarkProjectorItemForExlusion<T>() where T : ModItem, IProjector
        {

        }

        public static bool TryGetCreatorTagID(ReadOnlySpan<char> tag, out uint tagID)
        {
            tagID = CRC32.Calculate(tag);
            if (tagID == 0)
            {
                Projections.Log(LogType.Error, tag.Length < 1 ?
                    $"Could not register projector type! (Given tag was empty!)" :
                    $"Could not register projector type! (Given tag {tag} resulted in an ID of 0, try chaning it slightly)");
            }
            return tagID != 0;
        }
    }
}
