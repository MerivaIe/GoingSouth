using System.Collections;
using System.Collections.Generic;

public class MyUtility {

	//can't use Mathf.Sign because rounding errors introduced by Sin/Cos on float mean sometimes can't hit 0, so this custom function instead considers anything small enough to be 0
	public static int Sign(float value) {
		const float zeroBounds = 0.00001f;
		if (value > zeroBounds) {
			return 1;
		} else if (value < -zeroBounds) {
			return -1;
		} else {
			return 0;
		}
	}
}
