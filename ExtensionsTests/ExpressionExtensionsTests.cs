using System.Linq.Expressions;
using CommonExtensions;

namespace ExtensionsTests;

public class ExpressionExtensionsTests
{
    private static readonly TestObject Test = new(1, "One", Array.Empty<TestObject>());

    [Test]
    public void Test_GetCallParameters()
    {
        Expression<Func<TestObject, string>>? expression = Test.GetExpression(t => t.DoSomething(5));
        var expected = new Dictionary<string, object>
        {
            { "times", 5 }
        };
        IDictionary<string, object>? callParameters = expression.GetCallParameters();

        CollectionAssert.AreEqual(expected, callParameters);
    }

    [Test]
    public void Test_GetExpression_IsExpression()
    {
        Expression<Func<TestObject, string>>? expression = Test.GetExpression(t => t.Name);

        Assert.IsNotNull(expression);
        Assert.IsTrue(expression is Expression<Func<TestObject, string>>);
        Assert.That(expression.ToString(), Is.EqualTo("t => t.Name"));
    }

    [Test]
    public void Test_Text_Index_Property()
    {
        Expression<Func<TestObject, string>>? expression = Test.GetExpression(t => t.Children[0].Name);
        string expected = $"{nameof(TestObject.Children)}[0].{nameof(TestObject.Name)}";
        string? expressionText = expression.Text();

        CollectionAssert.AreEqual(expected, expressionText);
    }

    [Test]
    public void Test_Text_Property()
    {
        Expression<Func<TestObject, string>>? expression = Test.GetExpression(t => t.Name);
        string expected = nameof(TestObject.Name);
        string? expressionText = expression.Text();

        CollectionAssert.AreEqual(expected, expressionText);
    }

    private record TestObject(int Id, string Name, TestObject[] Children)
    {
        public string DoSomething(int times)
        {
            return (Id * times).ToString();
        }
    }
}