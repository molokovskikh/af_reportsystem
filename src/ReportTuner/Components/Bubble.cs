using Castle.MonoRail.Framework;

public class Bubble : ViewComponent
{
	public override void Render()
	{
		foreach (var key in ComponentParams.Keys)
		{
			Context.ContextVars[key] = ComponentParams[key];
			Context.ContextVars[key + ".@bubbleUp"] = true;
		}
	}
}