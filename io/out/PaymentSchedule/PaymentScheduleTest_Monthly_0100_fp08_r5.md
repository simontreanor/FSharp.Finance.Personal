<h2>PaymentScheduleTest_Monthly_0100_fp08_r5</h2>
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
        <td class="ci00">8</td>
        <td class="ci01" style="white-space: nowrap;">31.43</td>
        <td class="ci02">6.3840</td>
        <td class="ci03">6.38</td>
        <td class="ci04">25.05</td>
        <td class="ci05">0.00</td>
        <td class="ci06">74.95</td>
        <td class="ci07">6.3840</td>
        <td class="ci08">6.38</td>
        <td class="ci09">25.05</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">39</td>
        <td class="ci01" style="white-space: nowrap;">31.43</td>
        <td class="ci02">18.5411</td>
        <td class="ci03">18.54</td>
        <td class="ci04">12.89</td>
        <td class="ci05">0.00</td>
        <td class="ci06">62.06</td>
        <td class="ci07">24.9251</td>
        <td class="ci08">24.92</td>
        <td class="ci09">37.94</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">70</td>
        <td class="ci01" style="white-space: nowrap;">31.43</td>
        <td class="ci02">15.3524</td>
        <td class="ci03">15.35</td>
        <td class="ci04">16.08</td>
        <td class="ci05">0.00</td>
        <td class="ci06">45.98</td>
        <td class="ci07">40.2775</td>
        <td class="ci08">40.27</td>
        <td class="ci09">54.02</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">99</td>
        <td class="ci01" style="white-space: nowrap;">31.43</td>
        <td class="ci02">10.6407</td>
        <td class="ci03">10.64</td>
        <td class="ci04">20.79</td>
        <td class="ci05">0.00</td>
        <td class="ci06">25.19</td>
        <td class="ci07">50.9182</td>
        <td class="ci08">50.91</td>
        <td class="ci09">74.81</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">130</td>
        <td class="ci01" style="white-space: nowrap;">31.42</td>
        <td class="ci02">6.2315</td>
        <td class="ci03">6.23</td>
        <td class="ci04">25.19</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">57.1497</td>
        <td class="ci08">57.14</td>
        <td class="ci09">100.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0100 with 08 days to first payment and 5 repayments</i></p>
<p>Generated: <i>2025-04-16 using library version 2.1.2</i></p>
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
                    <td style="white-space: nowrap;">unit-period config: <i>monthly from 2023-12 on 15</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>57.14 %</i></td>
        <td>Initial APR: <i>1295.9 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>31.43</i></td>
        <td>Final payment: <i>31.42</i></td>
        <td>Final scheduled payment day: <i>130</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>157.14</i></td>
        <td>Total principal: <i>100.00</i></td>
        <td>Total interest: <i>57.14</i></td>
    </tr>
</table>
