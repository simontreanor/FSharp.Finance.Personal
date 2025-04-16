<h2>PaymentScheduleTest_Monthly_0100_fp16_r4</h2>
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
        <td class="ci00">16</td>
        <td class="ci01" style="white-space: nowrap;">38.02</td>
        <td class="ci02">12.7680</td>
        <td class="ci03">12.77</td>
        <td class="ci04">25.25</td>
        <td class="ci05">0.00</td>
        <td class="ci06">74.75</td>
        <td class="ci07">12.7680</td>
        <td class="ci08">12.77</td>
        <td class="ci09">25.25</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">47</td>
        <td class="ci01" style="white-space: nowrap;">38.02</td>
        <td class="ci02">18.4917</td>
        <td class="ci03">18.49</td>
        <td class="ci04">19.53</td>
        <td class="ci05">0.00</td>
        <td class="ci06">55.22</td>
        <td class="ci07">31.2597</td>
        <td class="ci08">31.26</td>
        <td class="ci09">44.78</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">78</td>
        <td class="ci01" style="white-space: nowrap;">38.02</td>
        <td class="ci02">13.6603</td>
        <td class="ci03">13.66</td>
        <td class="ci04">24.36</td>
        <td class="ci05">0.00</td>
        <td class="ci06">30.86</td>
        <td class="ci07">44.9200</td>
        <td class="ci08">44.92</td>
        <td class="ci09">69.14</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">107</td>
        <td class="ci01" style="white-space: nowrap;">38.00</td>
        <td class="ci02">7.1416</td>
        <td class="ci03">7.14</td>
        <td class="ci04">30.86</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">52.0616</td>
        <td class="ci08">52.06</td>
        <td class="ci09">100.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0100 with 16 days to first payment and 4 repayments</i></p>
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
                    <td style="white-space: nowrap;">unit-period config: <i>monthly from 2023-12 on 23</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>52.06 %</i></td>
        <td>Initial APR: <i>1309 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>38.02</i></td>
        <td>Final payment: <i>38.00</i></td>
        <td>Final scheduled payment day: <i>107</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>152.06</i></td>
        <td>Total principal: <i>100.00</i></td>
        <td>Total interest: <i>52.06</i></td>
    </tr>
</table>
