<h2>PaymentScheduleTest_Monthly_0100_fp28_r4</h2>
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
        <td class="ci06">100.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">28</td>
        <td class="ci01" style="white-space: nowrap;">41.13</td>
        <td class="ci02">22.3440</td>
        <td class="ci03">22.34</td>
        <td class="ci04">18.79</td>
        <td class="ci05">0.00</td>
        <td class="ci06">81.21</td>
        <td class="ci07">22.3440</td>
        <td class="ci08">22.34</td>
        <td class="ci09">18.79</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">59</td>
        <td class="ci01" style="white-space: nowrap;">41.13</td>
        <td class="ci02">20.0897</td>
        <td class="ci03">20.09</td>
        <td class="ci04">21.04</td>
        <td class="ci05">0.00</td>
        <td class="ci06">60.17</td>
        <td class="ci07">42.4337</td>
        <td class="ci08">42.43</td>
        <td class="ci09">39.83</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">88</td>
        <td class="ci01" style="white-space: nowrap;">41.13</td>
        <td class="ci02">13.9245</td>
        <td class="ci03">13.92</td>
        <td class="ci04">27.21</td>
        <td class="ci05">0.00</td>
        <td class="ci06">32.96</td>
        <td class="ci07">56.3583</td>
        <td class="ci08">56.35</td>
        <td class="ci09">67.04</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">119</td>
        <td class="ci01" style="white-space: nowrap;">41.11</td>
        <td class="ci02">8.1536</td>
        <td class="ci03">8.15</td>
        <td class="ci04">32.96</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">64.5119</td>
        <td class="ci08">64.50</td>
        <td class="ci09">100.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0100 with 28 days to first payment and 4 repayments</i></p>
<p>Generated: <i>2025-04-16 using library version 2.1.0</i></p>
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
        <td>100.00</td>
    </tr>
    <tr>
        <td>Schedule options</td>
        <td>
            <table>
                <tr>
                    <td>config: <i>auto-generate schedule</i></td>
                    <td>payment count: <i>4</i></td>
                </tr>
                <tr>
                    <td style="white-space: nowrap;">unit-period config: <i>monthly from 2024-01 on 04</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>64.5 %</i></td>
        <td>Initial APR: <i>1268.8 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>41.13</i></td>
        <td>Final payment: <i>41.11</i></td>
        <td>Final scheduled payment day: <i>119</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>164.50</i></td>
        <td>Total principal: <i>100.00</i></td>
        <td>Total interest: <i>64.50</i></td>
    </tr>
</table>
