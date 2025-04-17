<h2>PaymentScheduleTest_Monthly_0900_fp24_r5</h2>
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
        <td class="ci06">900.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">24</td>
        <td class="ci01" style="white-space: nowrap;">315.80</td>
        <td class="ci02">172.3680</td>
        <td class="ci03">172.37</td>
        <td class="ci04">143.43</td>
        <td class="ci05">0.00</td>
        <td class="ci06">756.57</td>
        <td class="ci07">172.3680</td>
        <td class="ci08">172.37</td>
        <td class="ci09">143.43</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">55</td>
        <td class="ci01" style="white-space: nowrap;">315.80</td>
        <td class="ci02">187.1603</td>
        <td class="ci03">187.16</td>
        <td class="ci04">128.64</td>
        <td class="ci05">0.00</td>
        <td class="ci06">627.93</td>
        <td class="ci07">359.5283</td>
        <td class="ci08">359.53</td>
        <td class="ci09">272.07</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">84</td>
        <td class="ci01" style="white-space: nowrap;">315.80</td>
        <td class="ci02">145.3156</td>
        <td class="ci03">145.32</td>
        <td class="ci04">170.48</td>
        <td class="ci05">0.00</td>
        <td class="ci06">457.45</td>
        <td class="ci07">504.8438</td>
        <td class="ci08">504.85</td>
        <td class="ci09">442.55</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">115</td>
        <td class="ci01" style="white-space: nowrap;">315.80</td>
        <td class="ci02">113.1640</td>
        <td class="ci03">113.16</td>
        <td class="ci04">202.64</td>
        <td class="ci05">0.00</td>
        <td class="ci06">254.81</td>
        <td class="ci07">618.0078</td>
        <td class="ci08">618.01</td>
        <td class="ci09">645.19</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">145</td>
        <td class="ci01" style="white-space: nowrap;">315.81</td>
        <td class="ci02">61.0015</td>
        <td class="ci03">61.00</td>
        <td class="ci04">254.81</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">679.0093</td>
        <td class="ci08">679.01</td>
        <td class="ci09">900.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0900 with 24 days to first payment and 5 repayments</i></p>
<p>Generated: <i>2025-04-17 using library version 2.2.0</i></p>
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
        <td>900.00</td>
    </tr>
    <tr>
        <td>Schedule options</td>
        <td>
            <table>
                <tr>
                    <td>config: <i>auto-generate schedule</i></td>
                    <td>schedule length: <i><i>payment count</i> 5</i></td>
                </tr>
                <tr>
                    <td colspan="2" style="white-space: nowrap;">unit-period config: <i>monthly from 2023-12 on month-end</i></td>
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
                </tr>
                <tr>
                    <td>rounding: <i>round using AwayFromZero</i></td>
                </tr>
                <tr>
                    <td>timeout: <i>3</i></td>
                </tr>
                <tr>
                    <td>minimum: <i>defer&nbsp;or&nbsp;write&nbsp;off&nbsp;up&nbsp;to&nbsp;0.50</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>75.45 %</i></td>
        <td>Initial APR: <i>1283.5 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>315.80</i></td>
        <td>Final payment: <i>315.81</i></td>
        <td>Final scheduled payment day: <i>145</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>1,579.01</i></td>
        <td>Total principal: <i>900.00</i></td>
        <td>Total interest: <i>679.01</i></td>
    </tr>
</table>
