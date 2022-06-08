﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.DrawerLayout.Widget;
using Google.Android.Material.AppBar;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;
using Xunit;
using AView = Android.Views.View;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Shell)]
	public partial class ShellTests
	{
		protected async Task CheckFlyoutState(ShellRenderer handler, bool desiredState)
		{
			var drawerLayout = GetDrawerLayout(handler);
			var flyout = drawerLayout.GetChildAt(1);

			if (drawerLayout.IsDrawerOpen(flyout) == desiredState)
			{
				Assert.Equal(desiredState, drawerLayout.IsDrawerOpen(flyout));
				return;
			}

			var taskCompletionSource = new TaskCompletionSource<bool>();
			flyout.LayoutChange += OnLayoutChanged;

			try
			{
				await taskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(2));
			}
			catch (TimeoutException)
			{

			}

			flyout.LayoutChange -= OnLayoutChanged;
			Assert.Equal(desiredState, drawerLayout.IsDrawerOpen(flyout));

			return;

			void OnLayoutChanged(object sender, Android.Views.View.LayoutChangeEventArgs e)
			{
				if (drawerLayout.IsDrawerOpen(flyout) == desiredState)
				{
					taskCompletionSource.SetResult(true);
					flyout.LayoutChange -= OnLayoutChanged;
				}
			}
		}

		[Fact(DisplayName = "FlyoutItems Render When FlyoutBehavior Starts As Locked")]
		public async Task FlyoutItemsRendererWhenFlyoutBehaviorStartsAsLocked()
		{
			SetupBuilder();
			var shell = await CreateShellAsync(shell =>
			{
				shell.CurrentItem = new FlyoutItem() { Items = { new ContentPage() }, Title = "Flyout Item" };
				shell.FlyoutBehavior = FlyoutBehavior.Locked;
			});

			await CreateHandlerAndAddToWindow<ShellRenderer>(shell, async (handler) =>
			{
				await Task.Delay(100);
				var dl = GetDrawerLayout(handler);
				var flyoutContainer = GetFlyoutMenuReyclerView(handler);

				Assert.True(flyoutContainer.MeasuredWidth > 0);
				Assert.True(flyoutContainer.MeasuredHeight > 0);
			});
		}


		[Fact(DisplayName = "Shell with Flyout Disabled Doesn't Render Flyout")]
		public async Task ShellWithFlyoutDisabledDoesntRenderFlyout()
		{
			SetupBuilder();
			var shell = await CreateShellAsync((shell) =>
			{
				shell.Items.Add(new ContentPage());
			});

			shell.FlyoutBehavior = FlyoutBehavior.Disabled;

			await CreateHandlerAndAddToWindow<ShellRenderer>(shell, (handler) =>
			{
				var dl = GetDrawerLayout(handler);
				Assert.Equal(1, dl.ChildCount);
				shell.FlyoutBehavior = FlyoutBehavior.Flyout;
				Assert.Equal(2, dl.ChildCount);
				return Task.CompletedTask;
			});
		}

		[Fact(DisplayName = "FooterTemplate Measures to Set Flyout Width When Flyout Locked")]
		public async Task FooterTemplateMeasuresToSetFlyoutWidth()
		{
			SetupBuilder();
			VerticalStackLayout footer = new VerticalStackLayout()
			{
				new Label(){ Text = "Hello there"}
			};

			var shell = await CreateShellAsync(shell =>
			{
				shell.CurrentItem = new FlyoutItem() { Items = { new ContentPage() }, Title = "Flyout Item" };
				shell.FlyoutBehavior = FlyoutBehavior.Locked;
				shell.FlyoutWidth = 20;
				shell.FlyoutFooter = footer;
			});

			await CreateHandlerAndAddToWindow<ShellRenderer>(shell, async (handler) =>
			{
				await OnFrameSetToNotEmpty(footer);
				Assert.True(Math.Abs(20 - footer.Frame.Width) < 1);
				Assert.True(footer.Frame.Height > 0);
			});
		}

		[Fact(DisplayName = "Flyout Footer and Default Flyout Items Render")]
		public async Task FlyoutFooterRenderersWithDefaultFlyoutItems()
		{
			SetupBuilder();
			VerticalStackLayout footer = new VerticalStackLayout()
			{
				new Label() { Text = "Hello there"}
			};

			var shell = await CreateShellAsync(shell =>
			{
				shell.CurrentItem = new FlyoutItem() { Items = { new ContentPage() }, Title = "Flyout Item" };
				shell.FlyoutFooter = footer;
			});

			await CreateHandlerAndAddToWindow<ShellRenderer>(shell, async (handler) =>
			{
				await Task.Delay(100);
				var dl = GetDrawerLayout(handler);
				await OpenDrawerLayout(handler);

				var flyoutContainer = GetFlyoutMenuReyclerView(handler);

				Assert.True(flyoutContainer.MeasuredWidth > 0);
				Assert.True(flyoutContainer.MeasuredHeight > 0);
			});
		}

		[Fact]
		public async Task FlyoutItemsRendererWhenFlyoutHeaderIsSet()
		{
			SetupBuilder();
			VerticalStackLayout header = new VerticalStackLayout()
			{
				new Label() { Text = "Hello there"}
			};

			var shell = await CreateShellAsync(shell =>
			{
				shell.CurrentItem = new FlyoutItem() { Items = { new ContentPage() }, Title = "Flyout Item" };
				shell.FlyoutHeader = header;
			});

			await CreateHandlerAndAddToWindow<ShellRenderer>(shell, async (handler) =>
			{
				await Task.Delay(100);
				var dl = GetDrawerLayout(handler);
				await OpenDrawerLayout(handler);

				var flyoutContainer = GetFlyoutMenuReyclerView(handler);

				Assert.True(flyoutContainer.MeasuredWidth > 0);
				Assert.True(flyoutContainer.MeasuredHeight > 0);
			});
		}

		[Fact]
		public async Task FlyoutHeaderRendersCorrectSizeWithFlyoutContentSet()
		{
			SetupBuilder();
			VerticalStackLayout header = new VerticalStackLayout()
			{
				new Label() { Text = "Flyout Header"}
			};

			var shell = await CreateShellAsync(shell =>
			{
				shell.CurrentItem = new FlyoutItem() { Items = { new ContentPage() }, Title = "Flyout Item" };
				shell.FlyoutHeader = header;

				shell.FlyoutContent = new VerticalStackLayout()
				{
					new Label(){ Text = "Flyout Content"}
				};

				shell.FlyoutFooter = new VerticalStackLayout()
				{
					new Label(){ Text = "Flyout Footer"}
				};
			});

			await CreateHandlerAndAddToWindow<ShellRenderer>(shell, async (handler) =>
			{
				await Task.Delay(100);
				var headerPlatformView = header.ToPlatform();
				var appBar = headerPlatformView.GetParentOfType<AppBarLayout>();
				Assert.Equal(appBar.MeasuredHeight, headerPlatformView.MeasuredHeight);
			});
		}

		protected Task OpenDrawerLayout(ShellRenderer shellRenderer, TimeSpan? timeOut = null)
		{
			var hamburger =
				GetPlatformToolbar((IPlatformViewHandler)shellRenderer).GetChildrenOfType<AppCompatImageButton>().FirstOrDefault() ??
				throw new InvalidOperationException("Unable to find Drawer Button");

			hamburger.PerformClick();

			var drawerLayout = GetDrawerLayout(shellRenderer);
			timeOut = timeOut ?? TimeSpan.FromSeconds(2);
			TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
			drawerLayout.DrawerOpened += OnDrawerOpened;

			return taskCompletionSource.Task.WaitAsync(timeOut.Value);

			void OnDrawerOpened(object sender, DrawerLayout.DrawerOpenedEventArgs e)
			{
				drawerLayout.DrawerOpened -= OnDrawerOpened;
				taskCompletionSource.SetResult(true);
			}
		}

		DrawerLayout GetDrawerLayout(ShellRenderer shellRenderer)
		{
			IShellContext shellContext = shellRenderer;
			return shellContext.CurrentDrawerLayout;
		}

		RecyclerViewContainer GetFlyoutMenuReyclerView(ShellRenderer shellRenderer)
		{
			IShellContext shellContext = shellRenderer;
			DrawerLayout dl = shellContext.CurrentDrawerLayout;

			var flyout = dl.GetChildAt(0);
			RecyclerViewContainer flyoutContainer = null;

			if (dl.GetChildAt(1) is ViewGroup vg1 &&
				vg1.GetChildAt(0) is RecyclerViewContainer rvc)
			{
				flyoutContainer = rvc;
			}

			return flyoutContainer ?? throw new Exception("RecyclerView not found");
		}
	}
}
