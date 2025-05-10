<h2>PaymentScheduleTest_Monthly_0300_fp24_r4</h2>
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
        <td class="ci06">300.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">24</td>
        <td class="ci01" style="white-space: nowrap;">120.17</td>
        <td class="ci02">57.4560</td>
        <td class="ci03">57.46</td>
        <td class="ci04">62.71</td>
        <td class="ci05">0.00</td>
        <td class="ci06">237.29</td>
        <td class="ci07">57.4560</td>
        <td class="ci08">57.46</td>
        <td class="ci09">62.71</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">55</td>
        <td class="ci01" style="white-space: nowrap;">120.17</td>
        <td class="ci02">58.7008</td>
        <td class="ci03">58.70</td>
        <td class="ci04">61.47</td>
        <td class="ci05">0.00</td>
        <td class="ci06">175.82</td>
        <td class="ci07">116.1568</td>
        <td class="ci08">116.16</td>
        <td class="ci09">124.18</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">84</td>
        <td class="ci01" style="white-space: nowrap;">120.17</td>
        <td class="ci02">40.6883</td>
        <td class="ci03">40.69</td>
        <td class="ci04">79.48</td>
        <td class="ci05">0.00</td>
        <td class="ci06">96.34</td>
        <td class="ci07">156.8451</td>
        <td class="ci08">156.85</td>
        <td class="ci09">203.66</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">115</td>
        <td class="ci01" style="white-space: nowrap;">120.17</td>
        <td class="ci02">23.8326</td>
        <td class="ci03">23.83</td>
        <td class="ci04">96.34</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">180.6777</td>
        <td class="ci08">180.68</td>
        <td class="ci09">300.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0300 with 24 days to first payment and 4 repayments</i></p>
<p>Generated: <i><a href="../GeneratedDate.md">see details</a></i></p>
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
        <td>Initial cost-to-borrowing ratio: <i>60.23 %</i></td>
        <td>Initial APR: <i>1287.8 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>120.17</i></td>
        <td>Final payment: <i>120.17</i></td>
        <td>Last scheduled payment day: <i>115</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>480.68</i></td>
        <td>Total principal: <i>300.00</i></td>
        <td>Total interest: <i>180.68</i></td>
    </tr>
</table>