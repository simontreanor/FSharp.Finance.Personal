<h2>PaymentScheduleTest_Monthly_0300_fp32_r4</h2>
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
        <td class="ci06">300.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">32</td>
        <td class="ci01" style="white-space: nowrap;">126.61</td>
        <td class="ci02">76.6080</td>
        <td class="ci03">76.61</td>
        <td class="ci04">50.00</td>
        <td class="ci05">0.00</td>
        <td class="ci06">250.00</td>
        <td class="ci07">76.6080</td>
        <td class="ci08">76.61</td>
        <td class="ci09">50.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">63</td>
        <td class="ci01" style="white-space: nowrap;">126.61</td>
        <td class="ci02">61.8450</td>
        <td class="ci03">61.85</td>
        <td class="ci04">64.76</td>
        <td class="ci05">0.00</td>
        <td class="ci06">185.24</td>
        <td class="ci07">138.4530</td>
        <td class="ci08">138.46</td>
        <td class="ci09">114.76</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">92</td>
        <td class="ci01" style="white-space: nowrap;">126.61</td>
        <td class="ci02">42.8682</td>
        <td class="ci03">42.87</td>
        <td class="ci04">83.74</td>
        <td class="ci05">0.00</td>
        <td class="ci06">101.50</td>
        <td class="ci07">181.3212</td>
        <td class="ci08">181.33</td>
        <td class="ci09">198.50</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">123</td>
        <td class="ci01" style="white-space: nowrap;">126.61</td>
        <td class="ci02">25.1091</td>
        <td class="ci03">25.11</td>
        <td class="ci04">101.50</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">206.4303</td>
        <td class="ci08">206.44</td>
        <td class="ci09">300.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0300 with 32 days to first payment and 4 repayments</i></p>
<p>Generated: <i>2025-05-08 using library version 2.4.4</i></p>
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
        <td>300.00</td>
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
                    <td colspan="2" style="white-space: nowrap;">unit-period config: <i>monthly from 2024-01 on 08</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>68.81 %</i></td>
        <td>Initial APR: <i>1248.6 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>126.61</i></td>
        <td>Final payment: <i>126.61</i></td>
        <td>Last scheduled payment day: <i>123</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>506.44</i></td>
        <td>Total principal: <i>300.00</i></td>
        <td>Total interest: <i>206.44</i></td>
    </tr>
</table>