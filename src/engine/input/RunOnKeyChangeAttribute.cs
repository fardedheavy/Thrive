using Godot;

/// <summary>
///   Attribute for a method, that gets called when the defined key is pressed or released.
///   Can be applied multiple times.
/// </summary>
public class RunOnKeyChangeAttribute : RunOnKeyAttribute
{
    public RunOnKeyChangeAttribute(string inputName) : base(inputName)
    {
    }

    public override bool OnInput(InputEvent @event)
    {
        var before = HeldDown;
        if (base.OnInput(@event) == before)
            return false;

        return CallMethod();
    }

    public override void OnProcess(float delta)
    {
    }
}
