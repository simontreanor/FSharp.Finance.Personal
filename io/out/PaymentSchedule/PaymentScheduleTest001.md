<h2>PaymentScheduleTest001</h2>
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
        <td class="ci00">15</td>
        <td class="ci01" style="white-space: nowrap;">336.00</td>
        <td class="ci02">36.0000</td>
        <td class="ci03">36.00</td>
        <td class="ci04">300.00</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">36.0000</td>
        <td class="ci08">36.00</td>
        <td class="ci09">300.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>If there are no other payments, level payment should equal final payment</i></p>
<p>Generated: <i>2025-04-28 using library version 2.2.10</i></p>
<h4>Parameters</h4>
<table>
    <tr>
        <td>Evaluation Date</td>
        <td>2022-12-19</td>
    </tr>
    <tr>
        <td>Start Date</td>
        <td>2022-12-19</td>
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
                    <td>schedule length: <i><i>payment count</i> 1</i></td>
                </tr>
                <tr>
                    <td colspan="2" style="white-space: nowrap;">unit-period config: <i>daily from 2023-01-03</i></td>
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
                    <td>rounding: <i>rounded up</i></td>
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
        <td>
            <table>
                <tr>
                    <th>Type</th>
                    <th>Charge</th>
                    <th>Grouping</th>
                    <th>Holidays</th>
                </tr>
                <tr>
                    <td>late payment</td>
                    <td>7.50</td><td>one charge per day</td><td><i>n/a</i></td>
                </tr>
            </table>
        </td>
    </tr>
    <tr>
        <td>Interest options</td>
        <td>
            <table>
                <tr>
                    <td>standard rate: <i>0.8 % per day</i></td>
                    <td>method: <i>simple</i></td>
                </tr>
                <tr>
                    <td>rounding: <i>rounded down</i></td>
                    <td>APR method: <i>UK FCA to 1 d.p.</i></td>
                </tr>
                <tr>
                    <td>initial grace period: <i>0 day(s)</i></td>
                    <td>rate on negative balance: <i>8 % per year</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>12 %</i></td>
        <td>Initial APR: <i>1476.3 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>336.00</i></td>
        <td>Final payment: <i>336.00</i></td>
        <td>Last scheduled payment day: <i>15</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>336.00</i></td>
        <td>Total principal: <i>300.00</i></td>
        <td>Total interest: <i>36.00</i></td>
    </tr>
</table>
