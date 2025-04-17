<h2>PaymentScheduleTest_Monthly_1300_fp24_r6</h2>
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
        <td class="ci06">1,300.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">24</td>
        <td class="ci01" style="white-space: nowrap;">414.91</td>
        <td class="ci02">248.9760</td>
        <td class="ci03">248.98</td>
        <td class="ci04">165.93</td>
        <td class="ci05">0.00</td>
        <td class="ci06">1,134.07</td>
        <td class="ci07">248.9760</td>
        <td class="ci08">248.98</td>
        <td class="ci09">165.93</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">55</td>
        <td class="ci01" style="white-space: nowrap;">414.91</td>
        <td class="ci02">280.5462</td>
        <td class="ci03">280.55</td>
        <td class="ci04">134.36</td>
        <td class="ci05">0.00</td>
        <td class="ci06">999.71</td>
        <td class="ci07">529.5222</td>
        <td class="ci08">529.53</td>
        <td class="ci09">300.29</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">84</td>
        <td class="ci01" style="white-space: nowrap;">414.91</td>
        <td class="ci02">231.3529</td>
        <td class="ci03">231.35</td>
        <td class="ci04">183.56</td>
        <td class="ci05">0.00</td>
        <td class="ci06">816.15</td>
        <td class="ci07">760.8751</td>
        <td class="ci08">760.88</td>
        <td class="ci09">483.85</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">115</td>
        <td class="ci01" style="white-space: nowrap;">414.91</td>
        <td class="ci02">201.8992</td>
        <td class="ci03">201.90</td>
        <td class="ci04">213.01</td>
        <td class="ci05">0.00</td>
        <td class="ci06">603.14</td>
        <td class="ci07">962.7743</td>
        <td class="ci08">962.78</td>
        <td class="ci09">696.86</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">145</td>
        <td class="ci01" style="white-space: nowrap;">414.91</td>
        <td class="ci02">144.3917</td>
        <td class="ci03">144.39</td>
        <td class="ci04">270.52</td>
        <td class="ci05">0.00</td>
        <td class="ci06">332.62</td>
        <td class="ci07">1,107.1660</td>
        <td class="ci08">1,107.17</td>
        <td class="ci09">967.38</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">176</td>
        <td class="ci01" style="white-space: nowrap;">414.90</td>
        <td class="ci02">82.2835</td>
        <td class="ci03">82.28</td>
        <td class="ci04">332.62</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">1,189.4496</td>
        <td class="ci08">1,189.45</td>
        <td class="ci09">1,300.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£1300 with 24 days to first payment and 6 repayments</i></p>
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
        <td>1,300.00</td>
    </tr>
    <tr>
        <td>Schedule options</td>
        <td>
            <table>
                <tr>
                    <td>config: <i>auto-generate schedule</i></td>
                    <td>payment count: <i>6</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>91.5 %</i></td>
        <td>Initial APR: <i>1280.4 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>414.91</i></td>
        <td>Final payment: <i>414.90</i></td>
        <td>Final scheduled payment day: <i>176</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>2,489.45</i></td>
        <td>Total principal: <i>1,300.00</i></td>
        <td>Total interest: <i>1,189.45</i></td>
    </tr>
</table>
