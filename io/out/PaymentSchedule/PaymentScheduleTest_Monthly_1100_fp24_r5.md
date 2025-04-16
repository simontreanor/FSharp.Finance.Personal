<h2>PaymentScheduleTest_Monthly_1100_fp24_r5</h2>
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
        <td class="ci06">1,100.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">24</td>
        <td class="ci01" style="white-space: nowrap;">385.98</td>
        <td class="ci02">210.6720</td>
        <td class="ci03">210.67</td>
        <td class="ci04">175.31</td>
        <td class="ci05">0.00</td>
        <td class="ci06">924.69</td>
        <td class="ci07">210.6720</td>
        <td class="ci08">210.67</td>
        <td class="ci09">175.31</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">55</td>
        <td class="ci01" style="white-space: nowrap;">385.98</td>
        <td class="ci02">228.7498</td>
        <td class="ci03">228.75</td>
        <td class="ci04">157.23</td>
        <td class="ci05">0.00</td>
        <td class="ci06">767.46</td>
        <td class="ci07">439.4218</td>
        <td class="ci08">439.42</td>
        <td class="ci09">332.54</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">84</td>
        <td class="ci01" style="white-space: nowrap;">385.98</td>
        <td class="ci02">177.6056</td>
        <td class="ci03">177.61</td>
        <td class="ci04">208.37</td>
        <td class="ci05">0.00</td>
        <td class="ci06">559.09</td>
        <td class="ci07">617.0274</td>
        <td class="ci08">617.03</td>
        <td class="ci09">540.91</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">115</td>
        <td class="ci01" style="white-space: nowrap;">385.98</td>
        <td class="ci02">138.3077</td>
        <td class="ci03">138.31</td>
        <td class="ci04">247.67</td>
        <td class="ci05">0.00</td>
        <td class="ci06">311.42</td>
        <td class="ci07">755.3351</td>
        <td class="ci08">755.34</td>
        <td class="ci09">788.58</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">145</td>
        <td class="ci01" style="white-space: nowrap;">385.97</td>
        <td class="ci02">74.5539</td>
        <td class="ci03">74.55</td>
        <td class="ci04">311.42</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">829.8890</td>
        <td class="ci08">829.89</td>
        <td class="ci09">1,100.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£1100 with 24 days to first payment and 5 repayments</i></p>
<p>Generated: <i>2025-04-16 using library version 2.1.1</i></p>
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
        <td>1,100.00</td>
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
                    <td style="white-space: nowrap;">unit-period config: <i>monthly from 2023-12 on month-end</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>75.44 %</i></td>
        <td>Initial APR: <i>1283.5 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>385.98</i></td>
        <td>Final payment: <i>385.97</i></td>
        <td>Final scheduled payment day: <i>145</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>1,929.89</i></td>
        <td>Total principal: <i>1,100.00</i></td>
        <td>Total interest: <i>829.89</i></td>
    </tr>
</table>
