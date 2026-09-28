using System;
using Avalonia;

namespace SheetMusicViewer.Desktop;

/// <summary>
/// Pure math for clamping the pinch/pan transform of a page viewport.
/// Deliberately free of UI controls so it can be unit tested on every platform.
///
/// The transformed content is assumed to be exactly the size of the viewport
/// when the transform is identity (the page bitmap is letterboxed inside by
/// the Image control's Uniform stretch).
/// </summary>
public static class GestureTransformMath
{
    public const double DefaultMinScale = 1.0;
    public const double DefaultMaxScale = 8.0;

    /// <summary>
    /// Clamps a scale factor, normalising NaN/infinity/zero to 1.
    /// </summary>
    public static double ClampScale(double scale, double minScale = DefaultMinScale, double maxScale = DefaultMaxScale)
    {
        if (double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0)
            return 1.0;

        return Math.Clamp(scale, minScale, maxScale);
    }

    /// <summary>
    /// Clamps a uniform scale+translation matrix so the content can never be
    /// dragged fully off-screen:
    /// <list type="bullet">
    /// <item>scale is clamped to [minScale, maxScale]</item>
    /// <item>when the scaled content is larger than the viewport, translation is
    /// limited so no gap can appear at the edges (no panning into empty space)</item>
    /// <item>when it is smaller, the content is centered</item>
    /// </list>
    /// Rotation/skew are not used by the gesture handler and are discarded.
    /// </summary>
    public static Matrix Clamp(Matrix matrix, Size viewport, double minScale = DefaultMinScale, double maxScale = DefaultMaxScale)
    {
        if (viewport.Width <= 0 || viewport.Height <= 0)
            return matrix;

        var scale = ClampScale(matrix.M11, minScale, maxScale);

        var scaledWidth  = viewport.Width  * scale;
        var scaledHeight = viewport.Height * scale;

        double tx;
        if (scaledWidth <= viewport.Width)
            tx = (viewport.Width - scaledWidth) / 2.0;          // center
        else
            tx = Math.Clamp(matrix.M31, viewport.Width - scaledWidth, 0);

        double ty;
        if (scaledHeight <= viewport.Height)
            ty = (viewport.Height - scaledHeight) / 2.0;        // center
        else
            ty = Math.Clamp(matrix.M32, viewport.Height - scaledHeight, 0);

        return new Matrix(scale, 0, 0, scale, tx, ty);
    }

    /// <summary>
    /// True when the matrix is (within a small epsilon) the identity transform,
    /// i.e. the page is shown fitted to the viewport.
    /// </summary>
    public static bool IsIdentity(Matrix matrix)
    {
        const double epsilon = 0.001;
        return Math.Abs(matrix.M11 - 1) < epsilon &&
               Math.Abs(matrix.M12) < epsilon &&
               Math.Abs(matrix.M21) < epsilon &&
               Math.Abs(matrix.M22 - 1) < epsilon &&
               Math.Abs(matrix.M31) < epsilon &&
               Math.Abs(matrix.M32) < epsilon;
    }
}
