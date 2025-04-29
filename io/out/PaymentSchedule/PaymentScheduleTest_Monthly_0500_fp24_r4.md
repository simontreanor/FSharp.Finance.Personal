<h2>PaymentScheduleTest_Monthly_0500_fp24_r4</h2>
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
        <td class="ci00">24</td>
        <td class="ci01" style="white-space: nowrap;">200.28</td>
        <td class="ci02">95.7600</td>
        <td class="ci03">95.76</td>
        <td class="ci04">104.52</td>
        <td class="ci05">0.00</td>
        <td class="ci06">395.48</td>
        <td class="ci07">95.7600</td>
        <td class="ci08">95.76</td>
        <td class="ci09">104.52</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">55</td>
        <td class="ci01" style="white-space: nowrap;">200.28</td>
        <td class="ci02">97.8338</td>
        <td class="ci03">97.83</td>
        <td class="ci04">102.45</td>
        <td class="ci05">0.00</td>
        <td class="ci06">293.03</td>
        <td class="ci07">193.5938</td>
        <td class="ci08">193.59</td>
        <td class="ci09">206.97</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">84</td>
        <td class="ci01" style="white-space: nowrap;">200.28</td>
        <td class="ci02">67.8130</td>
        <td class="ci03">67.81</td>
        <td class="ci04">132.47</td>
        <td class="ci05">0.00</td>
        <td class="ci06">160.56</td>
        <td class="ci07">261.4068</td>
        <td class="ci08">261.40</td>
        <td class="ci09">339.44</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">115</td>
        <td class="ci01" style="white-space: nowrap;">200.28</td>
        <td class="ci02">39.7193</td>
        <td class="ci03">39.72</td>
        <td class="ci04">160.56</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">301.1262</td>
        <td class="ci08">301.12</td>
        <td class="ci09">500.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0500 with 24 days to first payment and 4 repayments</i></p>
<p>Generated: <i>2025-04-29 using library version 2.3.0</i></p>
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
        <td>500.00</td>
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
                    <td>rounding: <i>round using AwayFromZero</i></td>
                </tr>
                <tr>
                    <td>level-payment option: <i>lower&nbsp;final&nbsp;payment</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>60.22 %</i></td>
        <td>Initial APR: <i>1287.6 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>200.28</i></td>
        <td>Final payment: <i>200.28</i></td>
        <td>Last scheduled payment day: <i>115</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>801.12</i></td>
        <td>Total principal: <i>500.00</i></td>
        <td>Total interest: <i>301.12</i></td>
    </tr>
</table>