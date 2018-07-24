using System.Collections.Generic;
using UnityEngine;

namespace DynamicTranslationLoader.Image
{
    internal class TextureMetadataComparer : IEqualityComparer<TextureMetadata>
    {
        public bool Equals(TextureMetadata x, TextureMetadata y)
        {
            return x.GetHashCode() == y.GetHashCode();
        }

        public int GetHashCode(TextureMetadata obj)
        {
            return obj.GetHashCode();
        }
    }

    internal struct TextureMetadata
    {
        public readonly Texture texture;
        public readonly string scene;

        public TextureMetadata(Texture texture, string gameObjectPath, string scene)
        {
            this.texture = texture;
            this.scene = scene;
        }

        public string ID => $"{texture.name}";

        public string SafeID => $"{texture.name}";

        public override int GetHashCode()
        {
            if (ID == null) return -1;
            return ID.GetHashCode();
        }
    }
}
