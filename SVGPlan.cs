//#define STOPWATCH
#if STOPWATCH
using System.Diagnostics;
#endif

using AndroidX.AppCompat.App;
using PlantRecorder.Utils;
using SkiaSharp;
using SkiaSharp.Views.Android;
using System;
using System.IO;

namespace PlantRecorder
{
	/// <summary>
	/// The Plan SVG class includes methods which are specific to a plan in SVG format.
	/// </summary>
	internal class SVGPlan : Plan
	{
		SKPicture planSKPicture = null;
		SkiaSharp.Extended.Svg.SKSvg SVGCanvas = null;


		/// <summary>
		/// The Plan constructor loads the plan from file and figures out its width and height.
		/// It also saves various variables which are needed later for displaying the plan.		
		/// The parent constructor is called with the canvasView. canvasView is not required in	
		/// this derived class.																
		/// </summary>
		/// <param name="activity"></param>
		/// <param name="canvasView"></param>
		/// <param name="planPathName"></param>
		internal SVGPlan(AppCompatActivity activity, SKCanvasView canvasView, string planPathName) : base(activity, canvasView)
		{
#if STOPWATCH
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
#endif
			// Create the SVG resource
			SVGCanvas = new SkiaSharp.Extended.Svg.SKSvg();

			// Check if SVG file exists. This should already have been done before getting here, but just in case...
			Java.IO.File planFile = new Java.IO.File(planPathName);
			if (planFile.Exists())
			{
				// Load the plan
				planSKPicture = loadPlan(planPathName);
				if (planSKPicture != null)
				{
					drawingWidth = planSKPicture.CullRect.Width;
					drawingHeight = planSKPicture.CullRect.Height;
					LogFile.WriteInfo(System.String.Format("planSKPicture size: {0} x {1}", drawingWidth, drawingHeight));

#if STOPWATCH
					stopwatch.Stop();
					LogFile.WriteInfo(System.String.Format("Path.LoadPlan(): {0}ms", stopwatch.ElapsedMilliseconds));
					stopwatch.Restart();
#endif
				} // if planSKPicture!=null
				else
				{
					// Do nothing. LoadPlan() throws an exception if plan can't be loaded.
					drawingWidth = 0;
					drawingHeight = 0;
				}
#if STOPWATCH
				stopwatch.Stop();
				LogFile.WriteInfo(System.String.Format("Plan() - completion: {0}ms", stopwatch.ElapsedMilliseconds));
#endif
			} // if (file.exists)
			else
			{
				String errorString = "File " + planPathName + " does not exist. ";
				LogFile.WriteError(errorString);
				throw new FileNotFoundException(errorString);
			}
		} // SVGPlan constructor


		/// <summary>
		/// This function reads the plan from an SVG file.  It is a helper function for the Plan constructor.                                    
		/// </summary>
		/// <param name="filePath">Full path incl. filename</param>
		/// <returns></returns>
		private SKPicture loadPlan(string filePath)
		{
			SKPicture LoadedPlan = null;

			try
			{
				LoadedPlan = SVGCanvas.Load(filePath);
			}
			catch (Exception ex)
			{
				string errorString = "Error loading SVG plan! ";
				LogFile.WriteError(errorString + ex.Message);
				throw new Exception(errorString, ex);
			}

			return LoadedPlan;
		} // loadPlan()


		/// <summary>
		/// This function is called by the parent class as part of the displayPlan() method.		
		/// It only implements the actual drawing of the plan, which is different depending on	
		/// whether an SVG or a JPEG plan is used.												
		/// </summary>
		/// <param name="canvas"></param>
		public override void drawPlan(SKCanvas canvas)
		{
			// Set up scaling matrix to zoom around pivot point at centre of display, then modify to take
			// account of translation. It would seem like a nice idea to set up the scaling matrix only
			// when we actually change the scale, but, actually, this gets extremely messy, because both
			// scaling and translating affect the "TransX" and "TransY" values of the matrix. See design
			// documentation.
			float scale = base.getCurrentScaleFactor();
			SKMatrix matrix = new SKMatrix(scale, 0, base.getTranslationX(), 0, scale, base.getTranslationY(), 0, 0, 1);

			try
			{
				// Draw the plan
				canvas.DrawPicture(SVGCanvas.Picture, ref matrix);
			}
			catch (Exception e)
			{
				string errorString = "Cannot draw SVG plan: ";
				LogFile.WriteError(errorString + e.Message);
				throw new Exception(errorString, e);
			}

			// Draw an outline around the plan, just so we can see where it is
			SKPaint paint = new SKPaint();
			// ***** There is an issue with SKColor; using pre-defined SkiaSharp colour, as it does not
			// seem to be possible to set SKColor to one of the colours defined in colors.xml. *****
			paint.Color = SKColors.Black;
			paint.IsStroke = true;
			paint.StrokeWidth = 2;
			// The transformation matrix was applied to the picture, not to the canvas, so we have to
			// apply the same transformtion to the frame.
			SKRect transformedRect = matrix.MapRect(SKRect.Create(0, 0, drawingWidth, drawingHeight));
			canvas.DrawRect(transformedRect, paint);
		} // drawPlan
	} // class SVGPlan
}