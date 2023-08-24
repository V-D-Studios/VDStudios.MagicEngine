using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.DemoResources;

public sealed class AnimationDefinition
{
    internal AnimationDefinition(int framecount, (int X, int Y, int Width, int Height)[] frames)
    {
        FrameCount = framecount;
        Frames = ImmutableArray.Create(frames);
    }

    public int FrameCount { get; }

    public ImmutableArray<(int X, int Y, int Width, int Height)> Frames { get; }
}

public static class AnimationDefinitions
{
    public static class Robin
    {
        public static class Enemy
        {
            private const int x = 0;

            public static AnimationDefinition Idle { get; }
                = new AnimationDefinition(4, GenAnim(x, 0));

            public static AnimationDefinition Active { get; }
                = new AnimationDefinition(4, GenAnim(x, 1));

            public static AnimationDefinition Left { get; }
                = new AnimationDefinition(4, GenAnim(x, 2));

            public static AnimationDefinition Right { get; }
                = new AnimationDefinition(4, GenAnim(x, 3));

            public static AnimationDefinition Down { get; }
                = new AnimationDefinition(4, GenAnim(x, 4));

            public static AnimationDefinition Up { get; }
                = new AnimationDefinition(4, GenAnim(x, 5));

            public static AnimationDefinition DownLeft { get; }
                = new AnimationDefinition(4, GenAnim(x, 6));

            public static AnimationDefinition DownRight { get; }
                = new AnimationDefinition(4, GenAnim(x, 7));

            public static AnimationDefinition UpRight { get; }
                = new AnimationDefinition(4, GenAnim(x, 8));

            public static AnimationDefinition UpLeft { get; }
                = new AnimationDefinition(4, GenAnim(x, 9));
        }

        public static class Player
        {
            private const int x = 1;

            public static AnimationDefinition Idle { get; }
                = new AnimationDefinition(4, GenAnim(x, 0));

            public static AnimationDefinition Active { get; }
                = new AnimationDefinition(4, GenAnim(x, 1));

            public static AnimationDefinition Left { get; }
                = new AnimationDefinition(4, GenAnim(x, 2));

            public static AnimationDefinition Right { get; }
                = new AnimationDefinition(4, GenAnim(x, 3));

            public static AnimationDefinition Down { get; }
                = new AnimationDefinition(4, GenAnim(x, 4));

            public static AnimationDefinition Up { get; }
                = new AnimationDefinition(4, GenAnim(x, 5));

            public static AnimationDefinition DownLeft { get; }
                = new AnimationDefinition(4, GenAnim(x, 6));

            public static AnimationDefinition DownRight { get; }
                = new AnimationDefinition(4, GenAnim(x, 7));

            public static AnimationDefinition UpRight { get; }
                = new AnimationDefinition(4, GenAnim(x, 8));

            public static AnimationDefinition UpLeft { get; }
                = new AnimationDefinition(4, GenAnim(x, 9));
        }

        public static class Ally
        {
            private const int x = 2;

            public static AnimationDefinition Idle { get; }
                = new AnimationDefinition(4, GenAnim(x, 0));

            public static AnimationDefinition Active { get; }
                = new AnimationDefinition(4, GenAnim(x, 1));

            public static AnimationDefinition Left { get; }
                = new AnimationDefinition(4, GenAnim(x, 2));

            public static AnimationDefinition Right { get; }
                = new AnimationDefinition(4, GenAnim(x, 3));

            public static AnimationDefinition Down { get; }
                = new AnimationDefinition(4, GenAnim(x, 4));

            public static AnimationDefinition Up { get; }
                = new AnimationDefinition(4, GenAnim(x, 5));

            public static AnimationDefinition DownLeft { get; }
                = new AnimationDefinition(4, GenAnim(x, 6));

            public static AnimationDefinition DownRight { get; }
                = new AnimationDefinition(4, GenAnim(x, 7));

            public static AnimationDefinition UpRight { get; }
                = new AnimationDefinition(4, GenAnim(x, 8));

            public static AnimationDefinition UpLeft { get; }
                = new AnimationDefinition(4, GenAnim(x, 9));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int X, int Y, int Width, int Height)[] GenAnim(int xOffset, int yOffset)
        {
            var x = new (int X, int Y, int Width, int Height)[]
            {
                (0 * 32 + xOffset * 128, 0 * 32 + yOffset * 32, 32, 32),
                (1 * 32 + xOffset * 128, 0 * 32 + yOffset * 32, 32, 32),
                (2 * 32 + xOffset * 128, 0 * 32 + yOffset * 32, 32, 32),
                (3 * 32 + xOffset * 128, 0 * 32 + yOffset * 32, 32, 32)
            };
            return x;
        }
    }
}
