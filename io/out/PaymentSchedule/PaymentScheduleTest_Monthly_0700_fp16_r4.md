<h2>PaymentScheduleTest_Monthly_0700_fp16_r4</h2>
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
        <td class="ci06">700.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">16</td>
        <td class="ci01" style="white-space: nowrap;">266.12</td>
        <td class="ci02">89.3760</td>
        <td class="ci03">89.38</td>
        <td class="ci04">176.74</td>
        <td class="ci05">0.00</td>
        <td class="ci06">523.26</td>
        <td class="ci07">89.3760</td>
        <td class="ci08">89.38</td>
        <td class="ci09">176.74</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">47</td>
        <td class="ci01" style="white-space: nowrap;">266.12</td>
        <td class="ci02">129.4441</td>
        <td class="ci03">129.44</td>
        <td class="ci04">136.68</td>
        <td class="ci05">0.00</td>
        <td class="ci06">386.58</td>
        <td class="ci07">218.8201</td>
        <td class="ci08">218.82</td>
        <td class="ci09">313.42</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">78</td>
        <td class="ci01" style="white-space: nowrap;">266.12</td>
        <td class="ci02">95.6322</td>
        <td class="ci03">95.63</td>
        <td class="ci04">170.49</td>
        <td class="ci05">0.00</td>
        <td class="ci06">216.09</td>
        <td class="ci07">314.4522</td>
        <td class="ci08">314.45</td>
        <td class="ci09">483.91</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">107</td>
        <td class="ci01" style="white-space: nowrap;">266.10</td>
        <td class="ci02">50.0075</td>
        <td class="ci03">50.01</td>
        <td class="ci04">216.09</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">364.4598</td>
        <td class="ci08">364.46</td>
        <td class="ci09">700.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0700 with 16 days to first payment and 4 repayments</i></p>
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
        <td>700.00</td>
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
                    <td colspan="2" style="white-space: nowrap;">unit-period config: <i>monthly from 2023-12 on 23</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>52.07 %</i></td>
        <td>Initial APR: <i>1309.1 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>266.12</i></td>
        <td>Final payment: <i>266.10</i></td>
        <td>Last scheduled payment day: <i>107</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>1,064.46</i></td>
        <td>Total principal: <i>700.00</i></td>
        <td>Total interest: <i>364.46</i></td>
    </tr>
</table>