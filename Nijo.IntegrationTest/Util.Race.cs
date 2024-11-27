using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.IntegrationTest {

	partial class Util {
		/// <summary>
		/// 2個の配列をマッチングキーで突合して、同じキーの組み合わせ同士で処理します。
		/// 例えばCSV2つをキーで突き合わせて差分を見たりするのに使うなど。
		/// </summary>
		/// <typeparam name="TItem">オブジェクトの型</typeparam>
		/// <param name="left">配列その1</param>
		/// <param name="right">配列その2</param>
		/// <param name="matchingKey">マッチングキー定義</param>
		/// <param name="whenMatch">キーが合致するものが両方の配列に存在した場合の処理</param>
		/// <param name="whenOnlyLeft">キーが合致するものが1つめの配列にのみに存在した場合の処理</param>
		/// <param name="whenOnlyRight">キーが合致するものが2つめの配列にのみに存在した場合の処理</param>
		public static void Race<TItem>(
			IEnumerable<TItem> left,
			IEnumerable<TItem> right,
			Func<TItem, string> matchingKey,
			Action<IEnumerable<TItem>, IEnumerable<TItem>>? whenMatch = null,
			Action<IEnumerable<TItem>>? whenOnlyLeft = null,
			Action<IEnumerable<TItem>>? whenOnlyRight = null) {

			var groupedLeft = left.GroupBy(matchingKey).OrderBy(group => group.Key).GetEnumerator();
			var groupedRight = right.GroupBy(matchingKey).OrderBy(group => group.Key).GetEnumerator();

			bool hasLeft = groupedLeft.MoveNext();
			bool hasRight = groupedRight.MoveNext();

			while (hasLeft && hasRight) {
				var leftGroup = groupedLeft.Current;
				var rightGroup = groupedRight.Current;

				int comparison = string.Compare(leftGroup.Key, rightGroup.Key);

				if (comparison == 0) {
					// キーが合致する場合の処理
					whenMatch?.Invoke(leftGroup, rightGroup);
					hasLeft = groupedLeft.MoveNext(); // 次の左側のグループに進む
					hasRight = groupedRight.MoveNext(); // 次の右側のグループに進む

				} else if (comparison < 0) {
					// 左側の配列にのみ存在する場合の処理
					whenOnlyLeft?.Invoke(leftGroup);
					hasLeft = groupedLeft.MoveNext(); // 次の左側のグループに進む

				} else {
					// 右側の配列にのみ存在する場合の処理
					whenOnlyRight?.Invoke(rightGroup);
					hasRight = groupedRight.MoveNext(); // 次の右側のグループに進む
				}
			}

			// 残りの左側の配列にのみ存在する場合の処理
			while (hasLeft) {
				whenOnlyLeft?.Invoke(groupedLeft.Current);
				hasLeft = groupedLeft.MoveNext(); // 次の左側のグループに進む
			}

			// 残りの右側の配列にのみ存在する場合の処理
			while (hasRight) {
				whenOnlyRight?.Invoke(groupedRight.Current);
				hasRight = groupedRight.MoveNext(); // 次の右側のグループに進む
			}
		}
	}
}
