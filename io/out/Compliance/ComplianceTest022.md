<h2>ComplianceTest022</h2>
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
        <td class="ci05">816.56</td>
        <td class="ci06">1,000.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">30</td>
        <td class="ci01" style="white-space: nowrap;">454.15</td>
        <td class="ci02">239.4000</td>
        <td class="ci03">454.15</td>
        <td class="ci04">0.00</td>
        <td class="ci05">362.41</td>
        <td class="ci06">1,000.00</td>
        <td class="ci07">239.4000</td>
        <td class="ci08">454.15</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">61</td>
        <td class="ci01" style="white-space: nowrap;">454.15</td>
        <td class="ci02">247.3800</td>
        <td class="ci03">362.41</td>
        <td class="ci04">91.74</td>
        <td class="ci05">0.00</td>
        <td class="ci06">908.26</td>
        <td class="ci07">486.7800</td>
        <td class="ci08">816.56</td>
        <td class="ci09">91.74</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">91</td>
        <td class="ci01" style="white-space: nowrap;">454.15</td>
        <td class="ci02">217.4374</td>
        <td class="ci03">0.00</td>
        <td class="ci04">454.15</td>
        <td class="ci05">0.00</td>
        <td class="ci06">454.11</td>
        <td class="ci07">704.2174</td>
        <td class="ci08">816.56</td>
        <td class="ci09">545.89</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">122</td>
        <td class="ci01" style="white-space: nowrap;">454.11</td>
        <td class="ci02">112.3377</td>
        <td class="ci03">0.00</td>
        <td class="ci04">454.11</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">816.5552</td>
        <td class="ci08">816.56</td>
        <td class="ci09">1,000.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>Add-on-interest loan of $1000 with payments starting after one month and 4 payments in total, for documentation purposes</i></p>
<p>Generated: <i>2025-04-28 using library version 2.2.10</i></p>
<h4>Parameters</h4>
<table>
    <tr>
        <td>Evaluation Date</td>
        <td>2025-04-22</td>
    </tr>
    <tr>
        <td>Start Date</td>
        <td>2025-04-22</td>
    </tr>
    <tr>
        <td>Principal</td>
        <td>1,000.00</td>
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
                    <td colspan="2" style="white-space: nowrap;">unit-period config: <i>monthly from 2025-05 on 22</i></td>
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
        <td>no charges
        </td>
    </tr>
    <tr>
        <td>Interest options</td>
        <td>
            <table>
                <tr>
                    <td>standard rate: <i>0.798 % per day</i></td>
                    <td>method: <i>add-on</i></td>
                </tr>
                <tr>
                    <td>rounding: <i>rounded down</i></td>
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
        <td>Initial interest balance: <i>816.56</i></td>
        <td>Initial cost-to-borrowing ratio: <i>81.66 %</i></td>
        <td>Initial APR: <i>2039.4 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>454.15</i></td>
        <td>Final payment: <i>454.11</i></td>
        <td>Last scheduled payment day: <i>122</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>1,816.56</i></td>
        <td>Total principal: <i>1,000.00</i></td>
        <td>Total interest: <i>816.56</i></td>
    </tr>
</table>
