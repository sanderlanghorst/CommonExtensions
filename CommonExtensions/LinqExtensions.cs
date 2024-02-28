using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonExtensions
{
    public static class LinqExtensions
    {
        /// <summary>
        /// combines an additional element with an enumerable
        /// </summary>
        /// <param name="source"></param>
        /// <param name="additional"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> And<T>(this IEnumerable<T> source, T additional)
        {
            foreach (T s in source)
            {
                yield return s;
            }

            yield return additional;
        }

        /// <summary>
        /// Combines two enumerables
        /// </summary>
        /// <param name="source"></param>
        /// <param name="additional"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> And<T>(this IEnumerable<T> source, IEnumerable<T> additional)
        {
            return new[] { source, additional }.SelectMany(e => e);
        }

        /// <summary>
        /// Divides the source enumerable into sized batches 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="batchSize"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            var batch = new List<T>();
            int c = 0;
            foreach (T e in source)
            {
                batch.Add(e);
                c++;
                if (c >= batchSize)
                {
                    yield return batch;
                    c = 0;
                    batch = new List<T>();
                }
            }

            if (batch.Any())
            {
                yield return batch;
            }
        }

        /// <summary>
        /// Joins two enumerables with each other, keeping default entries
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="leftKeySelection"></param>
        /// <param name="rightKeySelection"></param>
        /// <param name="resultSelection"></param>
        /// <typeparam name="TLeft"></typeparam>
        /// <typeparam name="TRight"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static IEnumerable<TResult> FullOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right,
            Func<TLeft, TKey> leftKeySelection,
            Func<TRight, TKey> rightKeySelection, Func<TLeft, TRight, TResult> resultSelection)
        {
            return left.LeftOuterJoin(right, leftKeySelection, rightKeySelection, (l, r) => (l, r))
                .Union(right.LeftOuterJoin(left, rightKeySelection, leftKeySelection, (r, l) => (l, r)))
                .Select(tuple => resultSelection(tuple.l, tuple.r));
        }

        /// <summary>
        /// groups a sequence of items. e.g. [1,2,3,6,7,8] => [[1,2,3],[6,7,8]]
        /// </summary>
        /// <param name="source">the source enumerable</param>
        /// <param name="sequenceCheck">checks two subsequent elements if they are within a sequence</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>a grouped enumerable</returns>
        public static IEnumerable<IEnumerable<T>> GroupSequence<T>(this IEnumerable<T> source, Func<T, T, bool> sequenceCheck)
        {
            var sub = new List<T>();
            using (IEnumerator<T> enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext()) yield break;

                T last = enumerator.Current;
                sub.Add(last);

                while (enumerator.MoveNext())
                {
                    //do
                    T current = enumerator.Current;
                    //check
                    if (!sequenceCheck(last, current))
                    {
                        yield return sub;
                        sub = new List<T> { current };
                    }
                    else
                    {
                        sub.Add(current);
                    }

                    //add or return
                    last = current;
                }

                yield return sub;
            }
        }

        /// <summary>
        /// Syntactic sugar for string.Join() to promote null coalescing
        /// </summary>
        /// <param name="source"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string Join(this IEnumerable<string> source, string separator)
        {
            return string.Join(separator, source);
        }

        /// <summary>
        /// Joins two enumerables to a tuple
        /// </summary>
        /// <param name="source"></param>
        /// <param name="joinee"></param>
        /// <param name="sourceKey"></param>
        /// <param name="joinKey"></param>
        /// <typeparam name="TS">The source type</typeparam>
        /// <typeparam name="T1">The extra tuple type</typeparam>
        /// <typeparam name="T2">The extra tuple type</typeparam>
        /// <typeparam name="TR">The joined result type</typeparam>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <returns></returns>
        public static IEnumerable<(TS, T1, T2, TR)> JoinZip<TS, T1, T2, TR, TKey>(this IEnumerable<(TS, T1, T2)> source, IEnumerable<TR> joinee, Func<TS, TKey> sourceKey,
            Func<TR, TKey> joinKey)
        {
            return source.GroupJoin(joinee, t => sourceKey(t.Item1), joinKey, (tuple, r) => (tuple.Item1, tuple.Item2, tuple.Item3, r))
                .SelectMany(xy => xy.r.DefaultIfEmpty(),
                    (t, r) => (t.Item1, t.Item2, t.Item3, r));
        }

        /// <summary>
        /// Joins two enumerables to a tuple
        /// </summary>
        /// <param name="source"></param>
        /// <param name="joinee"></param>
        /// <param name="sourceKey"></param>
        /// <param name="joinKey"></param>
        /// <typeparam name="TS">The source type</typeparam>
        /// <typeparam name="T1">The extra tuple type</typeparam>
        /// <typeparam name="TR">The joined result type</typeparam>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <returns></returns>
        public static IEnumerable<(TS, T1, TR)> JoinZip<TS, T1, TR, TKey>(this IEnumerable<(TS, T1)> source, IEnumerable<TR> joinee, Func<TS, TKey> sourceKey,
            Func<TR, TKey> joinKey)
        {
            return source.GroupJoin(joinee, t => sourceKey(t.Item1), joinKey, (tuple, r) => (tuple.Item1, tuple.Item2, r))
                .SelectMany(xy => xy.r.DefaultIfEmpty(),
                    (t, r) => (t.Item1, t.Item2, r));
        }

        /// <summary>
        /// Joins two enumerables to a tuple
        /// </summary>
        /// <param name="source"></param>
        /// <param name="joinee"></param>
        /// <param name="sourceKey"></param>
        /// <param name="joinKey"></param>
        /// <typeparam name="TS">The source type</typeparam>
        /// <typeparam name="TR">The joined result type</typeparam>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <returns></returns>
        public static IEnumerable<(TS, TR)> JoinZip<TS, TR, TKey>(this IEnumerable<TS> source, IEnumerable<TR> joinee, Func<TS, TKey> sourceKey, Func<TR, TKey> joinKey)
        {
            return source.GroupJoin(joinee, sourceKey, joinKey, (s, r) => (s, r))
                .SelectMany(xy => xy.r.DefaultIfEmpty(),
                    (t, r) => (t.s, r));
        }

        /// <summary>
        /// Joins two enumerables with each other, keeping the left and defaults the right side
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="leftKeySelection"></param>
        /// <param name="rightKeySelection"></param>
        /// <param name="resultSelection"></param>
        /// <typeparam name="TLeft"></typeparam>
        /// <typeparam name="TRight"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static IEnumerable<TResult> LeftOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right,
            Func<TLeft, TKey> leftKeySelection,
            Func<TRight, TKey> rightKeySelection, Func<TLeft, TRight, TResult> resultSelection)
        {
            return left.GroupJoin(right, leftKeySelection, rightKeySelection, (l, rs) => (l, rs))
                .SelectMany(tuple => tuple.rs.DefaultIfEmpty(), (tuple, r) => (tuple.l, r))
                .Select(tuple => resultSelection(tuple.l, tuple.r));
        }
    }
}