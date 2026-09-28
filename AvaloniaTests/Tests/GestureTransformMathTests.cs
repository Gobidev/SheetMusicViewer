using Avalonia;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SheetMusicViewer.Desktop;

namespace AvaloniaTests.Tests;

/// <summary>
/// Unit tests for the pinch/pan clamping math used by GestureHandler.
/// These are pure-math tests and do not require an Avalonia UI thread,
/// so they run on Windows, Linux and macOS.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class GestureTransformMathTests
{
    private static readonly Size Viewport = new(800, 600);

    private static Matrix ScaleAndTranslate(double scale, double tx, double ty)
        => new(scale, 0, 0, scale, tx, ty);

    [TestMethod]
    public void ClampScale_NormalisesInvalidValues()
    {
        Assert.AreEqual(1.0, GestureTransformMath.ClampScale(double.NaN));
        Assert.AreEqual(1.0, GestureTransformMath.ClampScale(double.PositiveInfinity));
        Assert.AreEqual(1.0, GestureTransformMath.ClampScale(0));
        Assert.AreEqual(1.0, GestureTransformMath.ClampScale(-3));
    }

    [TestMethod]
    public void ClampScale_RespectsBounds()
    {
        Assert.AreEqual(1.0, GestureTransformMath.ClampScale(0.5));
        Assert.AreEqual(8.0, GestureTransformMath.ClampScale(20));
        Assert.AreEqual(2.5, GestureTransformMath.ClampScale(2.5));
        Assert.AreEqual(0.5, GestureTransformMath.ClampScale(0.25, minScale: 0.5, maxScale: 4));
    }

    [TestMethod]
    public void Clamp_IdentityStaysIdentity()
    {
        var result = GestureTransformMath.Clamp(Matrix.Identity, Viewport);
        Assert.IsTrue(GestureTransformMath.IsIdentity(result));
    }

    [TestMethod]
    public void Clamp_ZoomedContentCannotBePannedPastEdges()
    {
        // 2x zoom on an 800x600 viewport = 1600x1200 content.
        // Translation must stay within [-800, 0] x [-600, 0] or a gap appears.
        var tooFarRight = GestureTransformMath.Clamp(ScaleAndTranslate(2, 300, 200), Viewport);
        Assert.AreEqual(0, tooFarRight.M31, 0.001);
        Assert.AreEqual(0, tooFarRight.M32, 0.001);

        var tooFarLeft = GestureTransformMath.Clamp(ScaleAndTranslate(2, -5000, -5000), Viewport);
        Assert.AreEqual(-800, tooFarLeft.M31, 0.001);
        Assert.AreEqual(-600, tooFarLeft.M32, 0.001);

        var inside = GestureTransformMath.Clamp(ScaleAndTranslate(2, -400, -300), Viewport);
        Assert.AreEqual(-400, inside.M31, 0.001);
        Assert.AreEqual(-300, inside.M32, 0.001);
    }

    [TestMethod]
    public void Clamp_ContentSmallerThanViewportIsCentered()
    {
        var result = GestureTransformMath.Clamp(
            ScaleAndTranslate(0.5, 0, 0), Viewport, minScale: 0.5);

        Assert.AreEqual(0.5, result.M11, 0.001);
        Assert.AreEqual(200, result.M31, 0.001); // (800 - 400) / 2
        Assert.AreEqual(150, result.M32, 0.001); // (600 - 300) / 2
    }

    [TestMethod]
    public void Clamp_ScaleIsClampedToMaximum()
    {
        var result = GestureTransformMath.Clamp(ScaleAndTranslate(100, 0, 0), Viewport);
        Assert.AreEqual(GestureTransformMath.DefaultMaxScale, result.M11, 0.001);
        Assert.AreEqual(GestureTransformMath.DefaultMaxScale, result.M22, 0.001);
    }

    [TestMethod]
    public void Clamp_DegenerateViewportReturnsInputUnchanged()
    {
        var matrix = ScaleAndTranslate(3, 10, 20);
        Assert.AreEqual(matrix, GestureTransformMath.Clamp(matrix, new Size(0, 0)));
        Assert.AreEqual(matrix, GestureTransformMath.Clamp(matrix, new Size(800, 0)));
    }

    [TestMethod]
    public void IsIdentity_DetectsTransformedMatrices()
    {
        Assert.IsTrue(GestureTransformMath.IsIdentity(Matrix.Identity));
        Assert.IsFalse(GestureTransformMath.IsIdentity(ScaleAndTranslate(1.1, 0, 0)));
        Assert.IsFalse(GestureTransformMath.IsIdentity(ScaleAndTranslate(1, 5, 0)));
        Assert.IsFalse(GestureTransformMath.IsIdentity(new Matrix(1, 0.2, 0, 1, 0, 0)));
    }
}
