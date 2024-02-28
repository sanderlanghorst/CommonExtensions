using CommonExtensions;

namespace ExtensionsTests;

public class LinqExtensionsTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test_And_list_int_OK()
    {
        int[] list = { 1, 2 };
        int[] expected = { 1, 2, 5 };
        IEnumerable<int>? result = list.And(5);
        CollectionAssert.AreEqual(expected, result);
    }

    [Test]
    public void Test_And_list_list_OK()
    {
        int[] list = { 1, 2 };
        int[] add = { 5, 6 };
        int[] expected = { 1, 2, 5, 6 };
        IEnumerable<int>? result = list.And(add);
        CollectionAssert.AreEqual(expected, result);
    }

    [Test]
    public void Test_Batch_List_BatchLargerThanList()
    {
        int[] list = Enumerable.Range(0, 10).ToArray();
        int batchSize = 50;
        int[][] expected = { list };

        IEnumerable<IEnumerable<int>>? result = list.Batch(batchSize);

        CollectionAssert.AreEqual(expected, result);
    }

    [Test]
    public void Test_Batch_List_ListEqualToBatch()
    {
        int[] list = Enumerable.Range(0, 10).ToArray();
        int batchSize = 10;
        int[][] expected = { list };

        IEnumerable<IEnumerable<int>>? result = list.Batch(batchSize);

        CollectionAssert.AreEqual(expected, result);
    }

    [Test]
    public void Test_Batch_List_ListLargerThanBatch()
    {
        int[] list = Enumerable.Range(0, 10).ToArray();
        int batchSize = 3;
        int[][] expected = { Enumerable.Range(0, 3).ToArray(), Enumerable.Range(3, 3).ToArray(), Enumerable.Range(6, 3).ToArray(), new[] { 9 } };

        IEnumerable<IEnumerable<int>>? result = list.Batch(batchSize);

        CollectionAssert.AreEqual(expected, result);
    }

    [Test]
    public void Test_FullOuterJoin_TestObjects()
    {
        TestObject defaultObject = default;
        TestObject[] listA = { new TestObject(1, "One"), new TestObject(2, "Two") };
        TestObject[] listB = { new TestObject(2, "B"), new TestObject(3, "C") };
        (TestObject?, TestObject?)[] expected = new[]
        {
            (new TestObject(1, "One"), defaultObject),
            (new TestObject(2, "Two"), new TestObject(2, "B")),
            (defaultObject, new TestObject(3, "C"))
        };

        var result = listA.FullOuterJoin(listB, a => a.Id, b => b.Id, (a, b) => (a, b)).ToList();

        CollectionAssert.AreEqual(expected, result);
    }

    [Test]
    public void Test_GroupSequence_intlist_OK()
    {
        int[] list = { 1, 2, 3, 5, 6, 7 };
        int[][] expected = { new[] { 1, 2, 3 }, new[] { 5, 6, 7 } };
        var result = list.GroupSequence((a, b) => b == a + 1).ToList();
        CollectionAssert.AreEqual(expected, result);
    }

    [Test]
    public void Test_Join_String()
    {
        string[] list = { "hello", "world" };
        string expected = "hello, world";
        string? result = list.Join(", ");

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Test_JoinZip_OneTwo()
    {
        TestObject[] listA = { new(1, "1"), new(2, "2") };
        TestObject[] listB = { new(1, "One"), new(2, "Two") };
        (TestObject, TestObject)[] expected = { (new TestObject(1, "1"), new TestObject(1, "One")), (new TestObject(2, "2"), new TestObject(2, "Two")) };

        IEnumerable<(TestObject, TestObject)>? result = listA.JoinZip(listB, a => a.Id, b => b.Id);
        CollectionAssert.AreEqual(expected, result);
    }

    [Test]
    public void Test_JoinZip_OneTwoThree()
    {
        TestObject[] listA = { new(1, "1"), new(2, "2") };
        TestObject[] listB = { new(1, "One"), new(2, "Two") };
        TestObject[] listC = { new(1, "A"), new(2, "B") };
        (TestObject, TestObject, TestObject)[] expected =
        {
            (new TestObject(1, "1"), new TestObject(1, "One"), new TestObject(1, "A")),
            (new TestObject(2, "2"), new TestObject(2, "Two"), new TestObject(2, "B"))
        };

        IEnumerable<(TestObject, TestObject, TestObject)>? result = listA.JoinZip(listB, a => a.Id, b => b.Id).JoinZip(listC, s => s.Id, c => c.Id);
        CollectionAssert.AreEqual(expected, result);
    }

    [Test]
    public void Test_JoinZip_OneTwoThreeFour()
    {
        TestObject[] listA = { new(1, "1"), new(2, "2") };
        TestObject[] listB = { new(1, "One"), new(2, "Two") };
        TestObject[] listC = { new(1, "A"), new(2, "B") };
        TestObject[] listD = { new(1, "C"), new(2, "D") };

        (TestObject, TestObject, TestObject, TestObject)[] expected =
        {
            (new TestObject(1, "1"), new TestObject(1, "One"), new TestObject(1, "A"), new TestObject(1, "C")),
            (new TestObject(2, "2"), new TestObject(2, "Two"), new TestObject(2, "B"), new TestObject(2, "D"))
        };

        IEnumerable<(TestObject, TestObject, TestObject, TestObject)>? result = listA
            .JoinZip(listB, a => a.Id, b => b.Id)
            .JoinZip(listC, s => s.Id, c => c.Id)
            .JoinZip(listD, s => s.Id, d => d.Id);

        CollectionAssert.AreEqual(expected, result);
    }

    [Test]
    public void Test_LeftOuterJoin_TestObjects()
    {
        TestObject defaultObject = default;
        TestObject[] listA = { new TestObject(1, "One"), new TestObject(2, "Two") };
        TestObject[] listB = { new TestObject(2, "B"), new TestObject(3, "C") };
        (TestObject, TestObject?)[] expected = new[]
        {
            (new TestObject(1, "One"), defaultObject),
            (new TestObject(2, "Two"), new TestObject(2, "B"))
        };
        IEnumerable<(TestObject a, TestObject b)>? result = listA.LeftOuterJoin(listB, a => a.Id, b => b.Id, (a, b) => (a, b));

        CollectionAssert.AreEqual(expected, result);
    }

    private record TestObject(int Id, string Name);
}