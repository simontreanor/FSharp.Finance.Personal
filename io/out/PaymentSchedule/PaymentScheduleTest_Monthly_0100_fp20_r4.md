<h2>PaymentScheduleTest_Monthly_0100_fp20_r4</h2>
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
        <td class="ci00">20</td>
        <td class="ci01" style="white-space: nowrap;">39.09</td>
        <td class="ci02">15.9600</td>
        <td class="ci03">15.96</td>
        <td class="ci04">23.13</td>
        <td class="ci05">0.00</td>
        <td class="ci06">76.87</td>
        <td class="ci07">15.9600</td>
        <td class="ci08">15.96</td>
        <td class="ci09">23.13</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">51</td>
        <td class="ci01" style="white-space: nowrap;">39.09</td>
        <td class="ci02">19.0161</td>
        <td class="ci03">19.02</td>
        <td class="ci04">20.07</td>
        <td class="ci05">0.00</td>
        <td class="ci06">56.80</td>
        <td class="ci07">34.9761</td>
        <td class="ci08">34.98</td>
        <td class="ci09">43.20</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">82</td>
        <td class="ci01" style="white-space: nowrap;">39.09</td>
        <td class="ci02">14.0512</td>
        <td class="ci03">14.05</td>
        <td class="ci04">25.04</td>
        <td class="ci05">0.00</td>
        <td class="ci06">31.76</td>
        <td class="ci07">49.0273</td>
        <td class="ci08">49.03</td>
        <td class="ci09">68.24</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">111</td>
        <td class="ci01" style="white-space: nowrap;">39.11</td>
        <td class="ci02">7.3499</td>
        <td class="ci03">7.35</td>
        <td class="ci04">31.76</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">56.3772</td>
        <td class="ci08">56.38</td>
        <td class="ci09">100.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0100 with 20 days to first payment and 4 repayments</i></p>
<p>Generated: <i>2025-05-02 using library version 2.3.1</i></p>
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
        <td>100.00</td>
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
                    <td colspan="2" style="white-space: nowrap;">unit-period config: <i>monthly from 2023-12 on 27</i></td>
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
                    <td>level-payment option: <i>similar&nbsp;final&nbsp;payment</i></td>
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
                    <td>method: <i>simple</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>56.38 %</i></td>
        <td>Initial APR: <i>1299.7 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>39.09</i></td>
        <td>Final payment: <i>39.11</i></td>
        <td>Last scheduled payment day: <i>111</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>156.38</i></td>
        <td>Total principal: <i>100.00</i></td>
        <td>Total interest: <i>56.38</i></td>
    </tr>
</table>