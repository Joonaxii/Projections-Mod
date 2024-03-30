
using Microsoft.Xna.Framework;

namespace Projections.Core.Data.Structures
{
    public struct RectF
    {
        public float x, y;
        public float width, height;

        public float Bottom => y;
        public float Top => y + height;
        public float Left => x;
        public float Right => x + width;

        public Vector2 Center => new Vector2(x + width * 0.5f, y + height * 0.5f);

        public RectF(Vector2 position, Vector2 size) : this(position.X, position.Y, size.X, size.Y)
        {

        }

        public RectF(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public static implicit operator Rectangle(RectF rect)
        {
            return new Rectangle((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
        }
        public static implicit operator RectF(Rectangle rect)
        {
            return new RectF(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public bool Contains(Vector2 value)
        {
            return x <= value.X && value.X < x + width && y <= value.Y && value.Y < y + height;
        }
        public bool Contains(float x, float y)
        {
            return this.x <= x && x < this.x + width && this.y <= y && y < this.y + height;
        }

        public bool Overlaps(ref RectF other)
        {
            return Overlaps(other.x, other.y, other.width, other.height);
        }

        public bool Overlaps(Vector2 position, Vector2 size)
        {
            return Overlaps(position.X, position.Y, size.X, size.Y);
        }

        public bool Overlaps(float x, float y, float width, float height)
        {
            return x < Right && Left < x + width && y < Bottom && Top < y + height;
        }
    }
}
