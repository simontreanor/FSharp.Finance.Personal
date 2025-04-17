<h2>PaymentScheduleTest_Monthly_1100_fp28_r5</h2>
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
        <td class="ci06">1,100.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">28</td>
        <td class="ci01" style="white-space: nowrap;">396.32</td>
        <td class="ci02">245.7840</td>
        <td class="ci03">245.78</td>
        <td class="ci04">150.54</td>
        <td class="ci05">0.00</td>
        <td class="ci06">949.46</td>
        <td class="ci07">245.7840</td>
        <td class="ci08">245.78</td>
        <td class="ci09">150.54</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">59</td>
        <td class="ci01" style="white-space: nowrap;">396.32</td>
        <td class="ci02">234.8774</td>
        <td class="ci03">234.88</td>
        <td class="ci04">161.44</td>
        <td class="ci05">0.00</td>
        <td class="ci06">788.02</td>
        <td class="ci07">480.6614</td>
        <td class="ci08">480.66</td>
        <td class="ci09">311.98</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">88</td>
        <td class="ci01" style="white-space: nowrap;">396.32</td>
        <td class="ci02">182.3636</td>
        <td class="ci03">182.36</td>
        <td class="ci04">213.96</td>
        <td class="ci05">0.00</td>
        <td class="ci06">574.06</td>
        <td class="ci07">663.0250</td>
        <td class="ci08">663.02</td>
        <td class="ci09">525.94</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">119</td>
        <td class="ci01" style="white-space: nowrap;">396.32</td>
        <td class="ci02">142.0110</td>
        <td class="ci03">142.01</td>
        <td class="ci04">254.31</td>
        <td class="ci05">0.00</td>
        <td class="ci06">319.75</td>
        <td class="ci07">805.0360</td>
        <td class="ci08">805.03</td>
        <td class="ci09">780.25</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">149</td>
        <td class="ci01" style="white-space: nowrap;">396.30</td>
        <td class="ci02">76.5482</td>
        <td class="ci03">76.55</td>
        <td class="ci04">319.75</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">881.5841</td>
        <td class="ci08">881.58</td>
        <td class="ci09">1,100.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£1100 with 28 days to first payment and 5 repayments</i></p>
<p>Generated: <i>2025-04-17 using library version 2.1.2</i></p>
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
        <td>1,100.00</td>
    </tr>
    <tr>
        <td>Schedule options</td>
        <td>
            <table>
                <tr>
                    <td>config: <i>auto-generate schedule</i></td>
                    <td>schedule length: <i><i>payment count</i> 5</i></td>
                </tr>
                <tr>
                    <td colspan="2" style="white-space: nowrap;">unit-period config: <i>monthly from 2024-01 on 04</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>80.14 %</i></td>
        <td>Initial APR: <i>1267.9 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>396.32</i></td>
        <td>Final payment: <i>396.30</i></td>
        <td>Final scheduled payment day: <i>149</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>1,981.58</i></td>
        <td>Total principal: <i>1,100.00</i></td>
        <td>Total interest: <i>881.58</i></td>
    </tr>
</table>
