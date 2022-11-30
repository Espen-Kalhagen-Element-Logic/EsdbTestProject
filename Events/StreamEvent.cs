namespace EsdbEvents;

public class StreamEvent
{
    public int StreamNr { get; set; }

    internal void When(object value)
    {
        var orderType = GetType();
        if (orderType == null)
            return;

        foreach (var prop in value.GetType().GetProperties())
        {
            if (prop == null)
                continue;

            var propGetter = prop.GetGetMethod();
            if (propGetter == null)
                continue;

            var propValue = orderType!.GetProperty(prop.Name);
            if (propValue == null)
                continue;

            var propSetter = propValue.GetSetMethod();
            if (propSetter == null)
                continue;

            var valueToSet = propGetter.Invoke(value, null);

            if (valueToSet != null)
                propSetter.Invoke(this, new[] { valueToSet });
        }
    }
}
