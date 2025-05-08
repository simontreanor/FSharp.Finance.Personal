<h2>PaymentScheduleTest_Monthly_0100_fp12_r4</h2>
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
        <td class="ci06">100.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">12</td>
        <td class="ci01" style="white-space: nowrap;">36.94</td>
        <td class="ci02">9.5760</td>
        <td class="ci03">9.58</td>
        <td class="ci04">27.36</td>
        <td class="ci05">0.00</td>
        <td class="ci06">72.64</td>
        <td class="ci07">9.5760</td>
        <td class="ci08">9.58</td>
        <td class="ci09">27.36</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">43</td>
        <td class="ci01" style="white-space: nowrap;">36.94</td>
        <td class="ci02">17.9697</td>
        <td class="ci03">17.97</td>
        <td class="ci04">18.97</td>
        <td class="ci05">0.00</td>
        <td class="ci06">53.67</td>
        <td class="ci07">27.5457</td>
        <td class="ci08">27.55</td>
        <td class="ci09">46.33</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">74</td>
        <td class="ci01" style="white-space: nowrap;">36.94</td>
        <td class="ci02">13.2769</td>
        <td class="ci03">13.28</td>
        <td class="ci04">23.66</td>
        <td class="ci05">0.00</td>
        <td class="ci06">30.01</td>
        <td class="ci07">40.8226</td>
        <td class="ci08">40.83</td>
        <td class="ci09">69.99</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">103</td>
        <td class="ci01" style="white-space: nowrap;">36.95</td>
        <td class="ci02">6.9449</td>
        <td class="ci03">6.94</td>
        <td class="ci04">30.01</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">47.7675</td>
        <td class="ci08">47.77</td>
        <td class="ci09">100.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0100 with 12 days to first payment and 4 repayments</i></p>
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
                    <td colspan="2" style="white-space: nowrap;">unit-period config: <i>monthly from 2023-12 on 19</i></td>
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
                    <td>level-payment option: <i>higher&nbsp;final&nbsp;payment</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>47.77 %</i></td>
        <td>Initial APR: <i>1312.3 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>36.94</i></td>
        <td>Final payment: <i>36.95</i></td>
        <td>Last scheduled payment day: <i>103</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>147.77</i></td>
        <td>Total principal: <i>100.00</i></td>
        <td>Total interest: <i>47.77</i></td>
    </tr>
</table>