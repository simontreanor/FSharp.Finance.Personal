<h2>PaymentScheduleTest_Monthly_0300_fp08_r5</h2>
<table>
    <thead style="vertical-align: bottom;">
        <th style="text-align: right;">Day</th>
        <th style="text-align: right;">Scheduled payment</th>
        <th style="text-align: right;">Simple interest</th>
        <th style="text-align: right;">Interest portion</th>
        <th style="text-align: right;">Principal portion</th>
        <th style="text-align: right;">Interest balance</th>
        <th style="text-align: right;">Principal balance</th>
        <th style="text-align: right;">Total simple interest</th>
        <th style="text-align: right;">Total interest</th>
        <th style="text-align: right;">Total principal</th>
    </thead>
    <tr style="text-align: right;">
        <td class="ci00">0</td>
        <td class="ci01" style="white-space: nowrap;">0.00</td>
        <td class="ci02">0.0000</td>
        <td class="ci03">0.00</td>
        <td class="ci04">0.00</td>
        <td class="ci05">0.00</td>
        <td class="ci06">300.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">8</td>
        <td class="ci01" style="white-space: nowrap;">94.29</td>
        <td class="ci02">19.1520</td>
        <td class="ci03">19.15</td>
        <td class="ci04">75.14</td>
        <td class="ci05">0.00</td>
        <td class="ci06">224.86</td>
        <td class="ci07">19.1520</td>
        <td class="ci08">19.15</td>
        <td class="ci09">75.14</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">39</td>
        <td class="ci01" style="white-space: nowrap;">94.29</td>
        <td class="ci02">55.6259</td>
        <td class="ci03">55.63</td>
        <td class="ci04">38.66</td>
        <td class="ci05">0.00</td>
        <td class="ci06">186.20</td>
        <td class="ci07">74.7779</td>
        <td class="ci08">74.78</td>
        <td class="ci09">113.80</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">70</td>
        <td class="ci01" style="white-space: nowrap;">94.29</td>
        <td class="ci02">46.0622</td>
        <td class="ci03">46.06</td>
        <td class="ci04">48.23</td>
        <td class="ci05">0.00</td>
        <td class="ci06">137.97</td>
        <td class="ci07">120.8400</td>
        <td class="ci08">120.84</td>
        <td class="ci09">162.03</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">99</td>
        <td class="ci01" style="white-space: nowrap;">94.29</td>
        <td class="ci02">31.9290</td>
        <td class="ci03">31.93</td>
        <td class="ci04">62.36</td>
        <td class="ci05">0.00</td>
        <td class="ci06">75.61</td>
        <td class="ci07">152.7690</td>
        <td class="ci08">152.77</td>
        <td class="ci09">224.39</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">130</td>
        <td class="ci01" style="white-space: nowrap;">94.31</td>
        <td class="ci02">18.7044</td>
        <td class="ci03">18.70</td>
        <td class="ci04">75.61</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">171.4734</td>
        <td class="ci08">171.47</td>
        <td class="ci09">300.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0300 with 08 days to first payment and 5 repayments</i></p>
<p>Generated: <i>2025-04-15 at 20:41:48</i></p>
<h4>Parameters</h4>
<table>
    <tr>
        <td>As-of</td>
        <td>2023-12-07</td>
    </tr>
    <tr>
        <td>Start</td>
        <td>2023-12-07</td>
    </tr>
    <tr>
        <td>Principal</td>
        <td>300.00</td>
    </tr>
    <tr>
        <td>Schedule options</td>
        <td>
            <table>
                <tr>
                    <td>config: <i>auto-generate schedule</i></td>
                    <td>payment count: <i>5</i></td>
                </tr>
                <tr>
                    <td style="white-space: nowrap;">unit-period config: <i>monthly from 2023-12 on 15</i></td>
                    <td>max duration: <i>unlimited</i></td>
                </tr>
            </table>
        </td>
    </tr>
    <tr>
        <td>Payment options</td>
        <td>
            <table>
                <tr>
                    <td>scheduling: <i>as scheduled</i></td>
                    <td>balance-close: <i>leave&nbsp;open&nbsp;balance</i></td>
                </tr>
                <tr>
                    <td>rounding: <i>round using AwayFromZero</i></td>
                    <td>timeout: <i>3</i></td>
                </tr>
                <tr>
                    <td colspan='2'>minimum: <i>defer&nbsp;or&nbsp;write&nbsp;off&nbsp;up&nbsp;to&nbsp;0.50</i></td>
                </tr>
                <tr>
                    <td colspan='2'>level-payment option: <i>similar&nbsp;final&nbsp;payment</i></td>
                </tr>
            </table>
        </td>
    </tr>
    <tr>
        <td>Fee options</td>
        <td>no fee
        </td>
    </tr>
    <tr>
        <td>Charge options</td>
        <td>no charges
        </td>
    </tr>
    <tr>
        <td>Interest options</td>
        <td>
            <table>
                <tr>
                    <td>standard rate: <i>0.798 % per day</i></td>
                    <td>method: <i>simple</i></td>
                </tr>
                <tr>
                    <td>rounding: <i>round using AwayFromZero</i></td>
                    <td>APR method: <i>UK FCA to 1 d.p.</i></td>
                </tr>
                <tr>
                    <td>initial grace period: <i>3 day(s)</i></td>
                    <td>rate on negative balance: <i>zero</i></td>
                </tr>
                <tr>
                    <td colspan="2">promotional rates: <i><i>n/a</i></i></td>
                </tr>
                <tr>
                    <td colspan="2">cap: <i>total 100 %; daily 0.8 %</td>
                </tr>
            </table>
        </td>
    </tr>
</table>
<h4>Initial Stats</h4>
<table>
    <tr>
        <td>Initial interest balance: <i>0.00</i></td>
        <td>Initial cost-to-borrowing ratio: <i>57.16 %</i></td>
        <td>Initial APR: <i>1296.5 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>94.29</i></td>
        <td>Final payment: <i>94.31</i></td>
        <td>Final scheduled payment day: <i>130</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>471.47</i></td>
        <td>Total principal: <i>300.00</i></td>
        <td>Total interest: <i>171.47</i></td>
    </tr>
</table>
