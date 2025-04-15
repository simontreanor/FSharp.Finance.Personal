<h2>PaymentScheduleTest_Monthly_0100_fp04_r5</h2>
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
        <td class="ci00">4</td>
        <td class="ci01" style="white-space: nowrap;">30.49</td>
        <td class="ci02">3.1920</td>
        <td class="ci03">3.19</td>
        <td class="ci04">27.30</td>
        <td class="ci05">0.00</td>
        <td class="ci06">72.70</td>
        <td class="ci07">3.1920</td>
        <td class="ci08">3.19</td>
        <td class="ci09">27.30</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">35</td>
        <td class="ci01" style="white-space: nowrap;">30.49</td>
        <td class="ci02">17.9845</td>
        <td class="ci03">17.98</td>
        <td class="ci04">12.51</td>
        <td class="ci05">0.00</td>
        <td class="ci06">60.19</td>
        <td class="ci07">21.1765</td>
        <td class="ci08">21.17</td>
        <td class="ci09">39.81</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">66</td>
        <td class="ci01" style="white-space: nowrap;">30.49</td>
        <td class="ci02">14.8898</td>
        <td class="ci03">14.89</td>
        <td class="ci04">15.60</td>
        <td class="ci05">0.00</td>
        <td class="ci06">44.59</td>
        <td class="ci07">36.0663</td>
        <td class="ci08">36.06</td>
        <td class="ci09">55.41</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">95</td>
        <td class="ci01" style="white-space: nowrap;">30.49</td>
        <td class="ci02">10.3190</td>
        <td class="ci03">10.32</td>
        <td class="ci04">20.17</td>
        <td class="ci05">0.00</td>
        <td class="ci06">24.42</td>
        <td class="ci07">46.3853</td>
        <td class="ci08">46.38</td>
        <td class="ci09">75.58</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">126</td>
        <td class="ci01" style="white-space: nowrap;">30.46</td>
        <td class="ci02">6.0410</td>
        <td class="ci03">6.04</td>
        <td class="ci04">24.42</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">52.4264</td>
        <td class="ci08">52.42</td>
        <td class="ci09">100.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0100 with 04 days to first payment and 5 repayments</i></p>
<p>Generated: <i>2025-04-15 at 20:41:49</i></p>
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
                    <td>payment count: <i>5</i></td>
                </tr>
                <tr>
                    <td style="white-space: nowrap;">unit-period config: <i>monthly from 2023-12 on 11</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>52.42 %</i></td>
        <td>Initial APR: <i>1280.8 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>30.49</i></td>
        <td>Final payment: <i>30.46</i></td>
        <td>Final scheduled payment day: <i>126</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>152.42</i></td>
        <td>Total principal: <i>100.00</i></td>
        <td>Total interest: <i>52.42</i></td>
    </tr>
</table>
