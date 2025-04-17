<h2>PaymentScheduleTest_Monthly_0500_fp04_r5</h2>
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
        <td class="ci06">500.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">4</td>
        <td class="ci01" style="white-space: nowrap;">152.44</td>
        <td class="ci02">15.9600</td>
        <td class="ci03">15.96</td>
        <td class="ci04">136.48</td>
        <td class="ci05">0.00</td>
        <td class="ci06">363.52</td>
        <td class="ci07">15.9600</td>
        <td class="ci08">15.96</td>
        <td class="ci09">136.48</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">35</td>
        <td class="ci01" style="white-space: nowrap;">152.44</td>
        <td class="ci02">89.9276</td>
        <td class="ci03">89.93</td>
        <td class="ci04">62.51</td>
        <td class="ci05">0.00</td>
        <td class="ci06">301.01</td>
        <td class="ci07">105.8876</td>
        <td class="ci08">105.89</td>
        <td class="ci09">198.99</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">66</td>
        <td class="ci01" style="white-space: nowrap;">152.44</td>
        <td class="ci02">74.4639</td>
        <td class="ci03">74.46</td>
        <td class="ci04">77.98</td>
        <td class="ci05">0.00</td>
        <td class="ci06">223.03</td>
        <td class="ci07">180.3514</td>
        <td class="ci08">180.35</td>
        <td class="ci09">276.97</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">95</td>
        <td class="ci01" style="white-space: nowrap;">152.44</td>
        <td class="ci02">51.6136</td>
        <td class="ci03">51.61</td>
        <td class="ci04">100.83</td>
        <td class="ci05">0.00</td>
        <td class="ci06">122.20</td>
        <td class="ci07">231.9650</td>
        <td class="ci08">231.96</td>
        <td class="ci09">377.80</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">126</td>
        <td class="ci01" style="white-space: nowrap;">152.43</td>
        <td class="ci02">30.2298</td>
        <td class="ci03">30.23</td>
        <td class="ci04">122.20</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">262.1949</td>
        <td class="ci08">262.19</td>
        <td class="ci09">500.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0500 with 04 days to first payment and 5 repayments</i></p>
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
        <td>500.00</td>
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
        <td>Initial cost-to-borrowing ratio: <i>52.44 %</i></td>
        <td>Initial APR: <i>1281.2 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>152.44</i></td>
        <td>Final payment: <i>152.43</i></td>
        <td>Final scheduled payment day: <i>126</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>762.19</i></td>
        <td>Total principal: <i>500.00</i></td>
        <td>Total interest: <i>262.19</i></td>
    </tr>
</table>
