using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using WolfManagement.Resources;

namespace UI
{
	[GlobalClass]
	public partial class Main : Control
	{
		[Export]
		public DockerController docker;
		[Export]
		public WolfConfig config;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
		}
	}
}