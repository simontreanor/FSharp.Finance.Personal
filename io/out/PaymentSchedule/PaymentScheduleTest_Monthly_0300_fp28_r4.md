<h2>PaymentScheduleTest_Monthly_0300_fp28_r4</h2>
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
        <td class="ci00">28</td>
        <td class="ci01" style="white-space: nowrap;">123.39</td>
        <td class="ci02">67.0320</td>
        <td class="ci03">67.03</td>
        <td class="ci04">56.36</td>
        <td class="ci05">0.00</td>
        <td class="ci06">243.64</td>
        <td class="ci07">67.0320</td>
        <td class="ci08">67.03</td>
        <td class="ci09">56.36</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">59</td>
        <td class="ci01" style="white-space: nowrap;">123.39</td>
        <td class="ci02">60.2717</td>
        <td class="ci03">60.27</td>
        <td class="ci04">63.12</td>
        <td class="ci05">0.00</td>
        <td class="ci06">180.52</td>
        <td class="ci07">127.3037</td>
        <td class="ci08">127.30</td>
        <td class="ci09">119.48</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">88</td>
        <td class="ci01" style="white-space: nowrap;">123.39</td>
        <td class="ci02">41.7759</td>
        <td class="ci03">41.78</td>
        <td class="ci04">81.61</td>
        <td class="ci05">0.00</td>
        <td class="ci06">98.91</td>
        <td class="ci07">169.0796</td>
        <td class="ci08">169.08</td>
        <td class="ci09">201.09</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">119</td>
        <td class="ci01" style="white-space: nowrap;">123.38</td>
        <td class="ci02">24.4684</td>
        <td class="ci03">24.47</td>
        <td class="ci04">98.91</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">193.5480</td>
        <td class="ci08">193.55</td>
        <td class="ci09">300.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0300 with 28 days to first payment and 4 repayments</i></p>
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
        <td>Initial cost-to-borrowing ratio: <i>64.52 %</i></td>
        <td>Initial APR: <i>1269.3 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>123.39</i></td>
        <td>Final payment: <i>123.38</i></td>
        <td>Final scheduled payment day: <i>119</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>493.55</i></td>
        <td>Total principal: <i>300.00</i></td>
        <td>Total interest: <i>193.55</i></td>
    </tr>
</table>
