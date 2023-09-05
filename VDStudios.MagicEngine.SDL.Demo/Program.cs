﻿using System.ComponentModel.DataAnnotations;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Internal;
using VDStudios.MagicEngine.SDL.Demo.Scenes;

namespace VDStudios.MagicEngine.SDL.Demo;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var game = new SDLGame();
        await game.StartGame(g => new TestScene(g));
    }
}
