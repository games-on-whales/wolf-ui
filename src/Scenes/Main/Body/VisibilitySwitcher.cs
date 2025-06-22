using Godot;

// Makes Sure at least on of its Control typed children Stays Visible. Runs in Game and in Editor

[Tool]
public partial class VisibilitySwitcher : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		CallDeferred(MethodName.DeferredReady);
	}

	private void DeferredReady()
	{
		var children = GetChildren();
		foreach(var child in children)
		{
			if(child is Control control)
			{
				control.VisibilityChanged += () => {
					if(control.Visible)
					{
						HideAllChildenExcept(control);
					}
					if(!control.Visible)
					{
						KeepOneChildVisible(control);
					}
				};
			}
		}
	}

	private void KeepOneChildVisible(Control changed)
	{
		var children = GetChildren();
		foreach(var child in children)
		{
			if(child is Control control && control.Visible)
			{
				return;
			}
		}

		CallDeferred(MethodName.SetVisible, changed);
	}

	private void SetVisible(Control control)
	{
		control.Visible = true;
	}

	private void HideAllChildenExcept(Control staysVisible)
	{
		var children = GetChildren();
		foreach(var child in children)
		{
			if(child is Control control && child != staysVisible)
			{
				control.Hide();
			}
		}
	}
}
