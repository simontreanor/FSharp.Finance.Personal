<h2>PaymentScheduleTest_Monthly_0500_fp08_r5</h2>
<table>
    <thead style="vertical-align: bottom;">
        <th style="text-align: right;">Day</th>
        <th style="text-align: right;">Scheduled payment</th>
        <th style="text-align: right;">Actuarial interest</th>
        <th style="text-align: right;">Interest portion</th>
        <th style="text-align: right;">Principal portion</th>
        <th style="text-align: right;">Interest balance</th>
        <th style="text-align: right;">Principal balance</th>
        <th style="text-align: right;">Total actuarial interest</th>
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
        <td class="ci06">500.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">8</td>
        <td class="ci01" style="white-space: nowrap;">157.16</td>
        <td class="ci02">31.9200</td>
        <td class="ci03">31.92</td>
        <td class="ci04">125.24</td>
        <td class="ci05">0.00</td>
        <td class="ci06">374.76</td>
        <td class="ci07">31.9200</td>
        <td class="ci08">31.92</td>
        <td class="ci09">125.24</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">39</td>
        <td class="ci01" style="white-space: nowrap;">157.16</td>
        <td class="ci02">92.7081</td>
        <td class="ci03">92.71</td>
        <td class="ci04">64.45</td>
        <td class="ci05">0.00</td>
        <td class="ci06">310.31</td>
        <td class="ci07">124.6281</td>
        <td class="ci08">124.63</td>
        <td class="ci09">189.69</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">70</td>
        <td class="ci01" style="white-space: nowrap;">157.16</td>
        <td class="ci02">76.7645</td>
        <td class="ci03">76.76</td>
        <td class="ci04">80.40</td>
        <td class="ci05">0.00</td>
        <td class="ci06">229.91</td>
        <td class="ci07">201.3926</td>
        <td class="ci08">201.39</td>
        <td class="ci09">270.09</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">99</td>
        <td class="ci01" style="white-space: nowrap;">157.16</td>
        <td class="ci02">53.2058</td>
        <td class="ci03">53.21</td>
        <td class="ci04">103.95</td>
        <td class="ci05">0.00</td>
        <td class="ci06">125.96</td>
        <td class="ci07">254.5984</td>
        <td class="ci08">254.60</td>
        <td class="ci09">374.04</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">130</td>
        <td class="ci01" style="white-space: nowrap;">157.12</td>
        <td class="ci02">31.1600</td>
        <td class="ci03">31.16</td>
        <td class="ci04">125.96</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">285.7584</td>
        <td class="ci08">285.76</td>
        <td class="ci09">500.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0500 with 08 days to first payment and 5 repayments</i></p>
<p>Generated: <i>2025-05-08 using library version 2.4.1</i></p>
<h4>Basic Parameters</h4>
<table>
    <tr>
        <td>Evaluation Date</td>
        <td>2023-12-07</td>
    </tr>
    <tr>
        <td>Start Date</td>
        <td>2023-12-07</td>
    </tr>
    <tr>
        <td>Principal</td>
        <td>500.00</td>
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
                    <td colspan="2" style="white-space: nowrap;">unit-period config: <i>monthly from 2023-12 on 15</i></td>
                </tr>
            </table>
        </td>
    </tr>
    <tr>
        <td>Payment options</td>
        <td>
            <table>
                <tr>
                    <td>rounding: <i>round using AwayFromZero</i></td>
                </tr>
                <tr>
                    <td>level-payment option: <i>lower&nbsp;final&nbsp;payment</i></td>
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
        <td>Interest options</td>
        <td>
            <table>
                <tr>
                    <td>standard rate: <i>0.798 % per day</i></td>
                    <td>method: <i>actuarial</i></td>
                </tr>
                <tr>
                    <td>rounding: <i>round using AwayFromZero</i></td>
                    <td>APR method: <i>UK FCA to 1 d.p.</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>57.15 %</i></td>
        <td>Initial APR: <i>1296.6 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>157.16</i></td>
        <td>Final payment: <i>157.12</i></td>
        <td>Last scheduled payment day: <i>130</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>785.76</i></td>
        <td>Total principal: <i>500.00</i></td>
        <td>Total interest: <i>285.76</i></td>
    </tr>
</table>