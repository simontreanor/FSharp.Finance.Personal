<h2>PaymentScheduleTest_Monthly_1300_fp32_r5</h2>
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
        <td class="ci06">1,300.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">32</td>
        <td class="ci01" style="white-space: nowrap;">480.60</td>
        <td class="ci02">331.9680</td>
        <td class="ci03">331.97</td>
        <td class="ci04">148.63</td>
        <td class="ci05">0.00</td>
        <td class="ci06">1,151.37</td>
        <td class="ci07">331.9680</td>
        <td class="ci08">331.97</td>
        <td class="ci09">148.63</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">63</td>
        <td class="ci01" style="white-space: nowrap;">480.60</td>
        <td class="ci02">284.8259</td>
        <td class="ci03">284.83</td>
        <td class="ci04">195.77</td>
        <td class="ci05">0.00</td>
        <td class="ci06">955.60</td>
        <td class="ci07">616.7939</td>
        <td class="ci08">616.80</td>
        <td class="ci09">344.40</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">92</td>
        <td class="ci01" style="white-space: nowrap;">480.60</td>
        <td class="ci02">221.1450</td>
        <td class="ci03">221.14</td>
        <td class="ci04">259.46</td>
        <td class="ci05">0.00</td>
        <td class="ci06">696.14</td>
        <td class="ci07">837.9389</td>
        <td class="ci08">837.94</td>
        <td class="ci09">603.86</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">123</td>
        <td class="ci01" style="white-space: nowrap;">480.60</td>
        <td class="ci02">172.2111</td>
        <td class="ci03">172.21</td>
        <td class="ci04">308.39</td>
        <td class="ci05">0.00</td>
        <td class="ci06">387.75</td>
        <td class="ci07">1,010.1500</td>
        <td class="ci08">1,010.15</td>
        <td class="ci09">912.25</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">153</td>
        <td class="ci01" style="white-space: nowrap;">480.58</td>
        <td class="ci02">92.8274</td>
        <td class="ci03">92.83</td>
        <td class="ci04">387.75</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">1,102.9773</td>
        <td class="ci08">1,102.98</td>
        <td class="ci09">1,300.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£1300 with 32 days to first payment and 5 repayments</i></p>
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
        <td>1,300.00</td>
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
                    <td style="white-space: nowrap;">unit-period config: <i>monthly from 2024-01 on 08</i></td>
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
                    <td colspan='2'>level-payment option: <i>lower&nbsp;final&nbsp;payment</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>84.84 %</i></td>
        <td>Initial APR: <i>1249.8 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>480.60</i></td>
        <td>Final payment: <i>480.58</i></td>
        <td>Final scheduled payment day: <i>153</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>2,402.98</i></td>
        <td>Total principal: <i>1,300.00</i></td>
        <td>Total interest: <i>1,102.98</i></td>
    </tr>
</table>
