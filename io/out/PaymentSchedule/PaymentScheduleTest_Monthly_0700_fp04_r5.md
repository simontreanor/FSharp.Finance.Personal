<h2>PaymentScheduleTest_Monthly_0700_fp04_r5</h2>
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
        <td class="ci00">4</td>
        <td class="ci01" style="white-space: nowrap;">213.42</td>
        <td class="ci02">22.3440</td>
        <td class="ci03">22.34</td>
        <td class="ci04">191.08</td>
        <td class="ci05">0.00</td>
        <td class="ci06">508.92</td>
        <td class="ci07">22.3440</td>
        <td class="ci08">22.34</td>
        <td class="ci09">191.08</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">35</td>
        <td class="ci01" style="white-space: nowrap;">213.42</td>
        <td class="ci02">125.8966</td>
        <td class="ci03">125.90</td>
        <td class="ci04">87.52</td>
        <td class="ci05">0.00</td>
        <td class="ci06">421.40</td>
        <td class="ci07">148.2406</td>
        <td class="ci08">148.24</td>
        <td class="ci09">278.60</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">66</td>
        <td class="ci01" style="white-space: nowrap;">213.42</td>
        <td class="ci02">104.2459</td>
        <td class="ci03">104.25</td>
        <td class="ci04">109.17</td>
        <td class="ci05">0.00</td>
        <td class="ci06">312.23</td>
        <td class="ci07">252.4866</td>
        <td class="ci08">252.49</td>
        <td class="ci09">387.77</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">95</td>
        <td class="ci01" style="white-space: nowrap;">213.42</td>
        <td class="ci02">72.2563</td>
        <td class="ci03">72.26</td>
        <td class="ci04">141.16</td>
        <td class="ci05">0.00</td>
        <td class="ci06">171.07</td>
        <td class="ci07">324.7428</td>
        <td class="ci08">324.75</td>
        <td class="ci09">528.93</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">126</td>
        <td class="ci01" style="white-space: nowrap;">213.39</td>
        <td class="ci02">42.3193</td>
        <td class="ci03">42.32</td>
        <td class="ci04">171.07</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">367.0621</td>
        <td class="ci08">367.07</td>
        <td class="ci09">700.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>£0700 with 04 days to first payment and 5 repayments</i></p>
<p>Generated: <i>2025-04-28 using library version 2.2.10</i></p>
<h4>Parameters</h4>
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
        <td>Initial APR: <i>1281.3 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>213.42</i></td>
        <td>Final payment: <i>213.39</i></td>
        <td>Last scheduled payment day: <i>126</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>1,067.07</i></td>
        <td>Total principal: <i>700.00</i></td>
        <td>Total interest: <i>367.07</i></td>
    </tr>
</table>
