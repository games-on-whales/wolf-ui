using Godot;
using System;

namespace UI
{
	public partial class ExitButton : Button
	{
		public override void _Ready()
		{
			Pressed += ()=>{
				GetTree().Quit();
			};
			SetProcess(false);
		}
	}
}