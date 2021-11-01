﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Singleton handling screenshot taking
/// </summary>
public class ScreenShotTaker : NodeWithInput
{
    private static ScreenShotTaker instance;
    private bool isCurrentlyTakingScreenshot;
    private Image screenshotImage;
    private Step step;
    private Task saveScreenshotTask;

    private ScreenShotTaker()
    {
        instance = this;
    }

    private enum Step
    {
        Start,
        Wait,
        TakeAndSaveScreenshot,
    }

    public static ScreenShotTaker Instance => instance;

    public override void _Ready()
    {
        // Keep this node running while paused
        PauseMode = PauseModeEnum.Process;
    }

    [RunOnKeyDown("screenshot", OnlyUnhandled = false)]
    public void TakeScreenshotPressed()
    {
        GD.Print("Taking a screenshot");
        TakeScreenshot();
    }

    /// <summary>
    ///   Takes an image of the current viewport
    /// </summary>
    /// <returns>The image</returns>
    public Image GetViewportTextureAsImage()
    {
        var image = GetViewport().GetTexture().GetData();

        // TODO: do we always need this?
        image.FlipY();

        return image;
    }

    private void SaveScreenshot(Image image)
    {
        FileHelpers.MakeSureDirectoryExists(Constants.SCREENSHOT_FOLDER);

        var filename = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss.ffff", CultureInfo.CurrentCulture) + ".png";

        var path = PathUtils.Join(Constants.SCREENSHOT_FOLDER, filename);

        var error = image.SavePng(path);

        if (error != Error.Ok)
        {
            GD.PrintErr("Saving screenshot failed ", error);
            return;
        }

        GD.Print("Saved screenshot: ", path);
    }

    /// <summary>
    ///   Takes and saves a screenshot
    /// </summary>
    private void TakeScreenshot()
    {
        if (isCurrentlyTakingScreenshot)
        {
            GD.Print("Already in the process of taking a screenshot.");
            return;
        }

        isCurrentlyTakingScreenshot = true;

        saveScreenshotTask = new Task(() =>
        {
            SaveScreenshot(screenshotImage);
            screenshotImage.Dispose();
            screenshotImage = null;
            isCurrentlyTakingScreenshot = false;
        });

        // If ScreenFilter is active, turn it off before taking a screenshot.
        if (ColourblindScreenFilter.Instance.Visible)
        {
            step = Step.Start;
            ScreenFilterScreenshotStepper();
            return;
        }

        screenshotImage = GetViewportTextureAsImage();
        TaskExecutor.Instance.AddTask(saveScreenshotTask);
    }

    /// <summary>
    ///   Invokes itself to:
    ///   1: Hide the ScreenFilter and wait a frame.
    ///   2: Wait another frame.
    ///   3: Take the screenshot, show the filter and then save
    ///      the screenshot in a task to not block the game.
    /// </summary>
    private void ScreenFilterScreenshotStepper()
    {
        switch (step)
        {
            case Step.Start:
                ColourblindScreenFilter.Instance.Hide();
                step = Step.Wait;
                break;
            case Step.Wait:
                step = Step.TakeAndSaveScreenshot;
                break;
            case Step.TakeAndSaveScreenshot:
                screenshotImage = GetViewportTextureAsImage();
                ColourblindScreenFilter.Instance.Show();
                TaskExecutor.Instance.AddTask(saveScreenshotTask);
                return;
        }

        Invoke.Instance.Queue(ScreenFilterScreenshotStepper);
    }
}
