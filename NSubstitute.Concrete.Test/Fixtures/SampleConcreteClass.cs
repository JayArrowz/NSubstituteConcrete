namespace NSubstitute.Concrete.Test.Fixtures;

public class SampleConcreteClass
{
    public int Id { get; }
    public string Name { get; set; }

    public SampleConcreteClass(int id) => Id = id;

    public int IncrementAndReturn(int increment) => Id + increment;

    public virtual int AVirtualMethod() => Id + 1;

    public Task<int> GetDataAsync(int id) => Task.FromResult(id);

    public void DoSomething(int a, string b) { }

    public void Abc(int a) { }

    public Task DoSomethingAsync() => Task.CompletedTask;
}
