<h2>PaymentScheduleTest_Monthly_0300_fp04_r5</h2>
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
        <td class="ci06">300.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">4</td>
        <td class="ci01" style="white-space: nowrap;">91.46</td>
        <td class="ci02">9.5760</td>
        <td class="ci03">9.58</td>
        <td class="ci04">81.88</td>
        <td class="ci05">0.00</td>
        <td class="ci06">218.12</td>
        <td class="ci07">9.5760</td>
        <td class="ci08">9.58</td>
        <td class="ci09">81.88</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">35</td>
        <td class="ci01" style="white-space: nowrap;">91.46</td>
        <td class="ci02">53.9585</td>
        <td class="ci03">53.96</td>
        <td class="ci04">37.50</td>
        <td class="ci05">0.00</td>
        <td class="ci06">180.62</td>
        <td class="ci07">63.5345</td>
        <td class="ci08">63.54</td>
        <td class="ci09">119.38</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">66</td>
        <td class="ci01" style="white-space: nowrap;">91.46</td>
        <td class="ci02">44.6818</td>
        <td class="ci03">44.68</td>
        <td class="ci04">46.78</td>
        <td class="ci05">0.00</td>
        <td class="ci06">133.84</td>
        <td class="ci07">108.2163</td>
        <td class="ci08">108.22</td>
        <td class="ci09">166.16</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">95</td>
        <td class="ci01" style="white-space: nowrap;">91.46</td>
        <td class="ci02">30.9733</td>
        <td class="ci03">30.97</td>
        <td class="ci04">60.49</td>
        <td class="ci05">0.00</td>
        <td class="ci06">73.35</td>
        <td class="ci07">139.1896</td>
        <td class="ci08">139.19</td>
        <td class="ci09">226.65</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">126</td>
        <td class="ci01" style="white-space: nowrap;">91.50</td>
        <td class="ci02">18.1453</td>
        <td class="ci03">18.15</td>
        <td class="ci04">73.35</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">157.3349</td>
        <td class="ci08">157.34</td>
        <td class="ci09">300.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0300 with 04 days to first payment and 5 repayments</i></p>
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
        <td>300.00</td>
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
                    <td colspan="2" style="white-space: nowrap;">unit-period config: <i>monthly from 2023-12 on 11</i></td>
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
                    <td colspan='2'>level-payment option: <i>higher&nbsp;final&nbsp;payment</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>52.45 %</i></td>
        <td>Initial APR: <i>1281.4 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>91.46</i></td>
        <td>Final payment: <i>91.50</i></td>
        <td>Final scheduled payment day: <i>126</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>457.34</i></td>
        <td>Total principal: <i>300.00</i></td>
        <td>Total interest: <i>157.34</i></td>
    </tr>
</table>
