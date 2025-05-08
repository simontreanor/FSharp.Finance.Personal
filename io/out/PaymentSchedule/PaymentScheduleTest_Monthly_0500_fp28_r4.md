<h2>PaymentScheduleTest_Monthly_0500_fp28_r4</h2>
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
        <td class="ci00">28</td>
        <td class="ci01" style="white-space: nowrap;">205.65</td>
        <td class="ci02">111.7200</td>
        <td class="ci03">111.72</td>
        <td class="ci04">93.93</td>
        <td class="ci05">0.00</td>
        <td class="ci06">406.07</td>
        <td class="ci07">111.7200</td>
        <td class="ci08">111.72</td>
        <td class="ci09">93.93</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">59</td>
        <td class="ci01" style="white-space: nowrap;">205.65</td>
        <td class="ci02">100.4536</td>
        <td class="ci03">100.45</td>
        <td class="ci04">105.20</td>
        <td class="ci05">0.00</td>
        <td class="ci06">300.87</td>
        <td class="ci07">212.1736</td>
        <td class="ci08">212.17</td>
        <td class="ci09">199.13</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">88</td>
        <td class="ci01" style="white-space: nowrap;">205.65</td>
        <td class="ci02">69.6273</td>
        <td class="ci03">69.63</td>
        <td class="ci04">136.02</td>
        <td class="ci05">0.00</td>
        <td class="ci06">164.85</td>
        <td class="ci07">281.8009</td>
        <td class="ci08">281.80</td>
        <td class="ci09">335.15</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">119</td>
        <td class="ci01" style="white-space: nowrap;">205.63</td>
        <td class="ci02">40.7806</td>
        <td class="ci03">40.78</td>
        <td class="ci04">164.85</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">322.5815</td>
        <td class="ci08">322.58</td>
        <td class="ci09">500.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0500 with 28 days to first payment and 4 repayments</i></p>
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
                    <td>schedule length: <i><i>payment count</i> 4</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>64.52 %</i></td>
        <td>Initial APR: <i>1269.3 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>205.65</i></td>
        <td>Final payment: <i>205.63</i></td>
        <td>Last scheduled payment day: <i>119</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>822.58</i></td>
        <td>Total principal: <i>500.00</i></td>
        <td>Total interest: <i>322.58</i></td>
    </tr>
</table>