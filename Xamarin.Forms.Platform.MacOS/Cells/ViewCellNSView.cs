﻿using System;
using AppKit;

namespace Xamarin.Forms.Platform.MacOS
{
	public class ViewCellNSView : NSView, INativeElementView
	{
		WeakReference<IVisualElementRenderer> _rendererRef;

		ViewCell _viewCell;

		public ViewCellNSView(string key)
		{
		}

		public ViewCell ViewCell
		{
			get { return _viewCell; }
			set
			{
				if (_viewCell == value)
					return;
				UpdateCell(value);
			}
		}

		Element INativeElementView.Element
		{
			get { return ViewCell; }
		}

		public override void Layout()
		{
			base.Layout();
			LayoutSubviews();
		}

		public void LayoutSubviews()
		{
			var contentFrame = Frame;
			var view = ViewCell.View;

			Xamarin.Forms.Layout.LayoutChildIntoBoundingRegion(view, contentFrame.ToRectangle());

			if (_rendererRef == null)
				return;

			IVisualElementRenderer renderer;
			if (_rendererRef.TryGetTarget(out renderer))
				renderer.NativeView.Frame = view.Bounds.ToRectangleF();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				IVisualElementRenderer renderer;
				if (_rendererRef != null && _rendererRef.TryGetTarget(out renderer) && renderer.Element != null)
				{
					var platform = renderer.Element.Platform as Platform;
					if (platform != null)
						platform.DisposeModelAndChildrenRenderers(renderer.Element);

					_rendererRef = null;
				}
			}

			base.Dispose(disposing);
		}

		IVisualElementRenderer GetNewRenderer()
		{
			var newRenderer = Platform.CreateRenderer(_viewCell.View);
			_rendererRef = new WeakReference<IVisualElementRenderer>(newRenderer);
			AddSubview(newRenderer.NativeView);
			return newRenderer;
		}

		void UpdateCell(ViewCell cell)
		{
			ICellController cellController = _viewCell;
			if (cellController != null)
				Device.BeginInvokeOnMainThread(cellController.SendDisappearing);

			_viewCell = cell;
			cellController = cell;

			Device.BeginInvokeOnMainThread(cellController.SendAppearing);

			IVisualElementRenderer renderer;
			if (_rendererRef == null || !_rendererRef.TryGetTarget(out renderer))
				renderer = GetNewRenderer();
			else
			{
				if (renderer.Element != null && renderer == Platform.GetRenderer(renderer.Element))
					renderer.Element.ClearValue(Platform.RendererProperty);

				var type = Registrar.Registered.GetHandlerType(_viewCell.View.GetType());
				if (renderer.GetType() == type || (renderer is DefaultRenderer && type == null))
					renderer.SetElement(_viewCell.View);
				else
				{
					//when cells are getting reused the element could be already set to another cell
					//so we should dispose based on the renderer and not the renderer.Element
					var platform = renderer.Element.Platform as Platform;
					platform.DisposeRendererAndChildren(renderer);
					renderer = GetNewRenderer();
				}
			}

			Platform.SetRenderer(_viewCell.View, renderer);
		}
	}
}
