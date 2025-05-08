<h2>ComplianceTest023</h2>
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
        <td class="ci06">1,000.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">30</td>
        <td class="ci01" style="white-space: nowrap;">417.72</td>
        <td class="ci02">239.4000</td>
        <td class="ci03">239.40</td>
        <td class="ci04">178.32</td>
        <td class="ci05">0.00</td>
        <td class="ci06">821.68</td>
        <td class="ci07">239.4000</td>
        <td class="ci08">239.40</td>
        <td class="ci09">178.32</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">61</td>
        <td class="ci01" style="white-space: nowrap;">417.72</td>
        <td class="ci02">203.2672</td>
        <td class="ci03">203.26</td>
        <td class="ci04">214.46</td>
        <td class="ci05">0.00</td>
        <td class="ci06">607.22</td>
        <td class="ci07">442.6672</td>
        <td class="ci08">442.66</td>
        <td class="ci09">392.78</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">91</td>
        <td class="ci01" style="white-space: nowrap;">417.72</td>
        <td class="ci02">145.3685</td>
        <td class="ci03">145.36</td>
        <td class="ci04">272.36</td>
        <td class="ci05">0.00</td>
        <td class="ci06">334.86</td>
        <td class="ci07">588.0357</td>
        <td class="ci08">588.02</td>
        <td class="ci09">665.14</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">122</td>
        <td class="ci01" style="white-space: nowrap;">417.69</td>
        <td class="ci02">82.8377</td>
        <td class="ci03">82.83</td>
        <td class="ci04">334.86</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">670.8733</td>
        <td class="ci08">670.85</td>
        <td class="ci09">1,000.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>Actuarial-interest loan of $1000 with payments starting after one month and 4 payments in total, for documentation purposes</i></p>
<p>Generated: <i>2025-05-08 using library version 2.4.4</i></p>
<h4>Basic Parameters</h4>
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
                    <td>rounding: <i>rounded up</i></td>
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
                    <td>method: <i>add-on</i></td>
                </tr>
                <tr>
                    <td>rounding: <i>rounded down</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>67.09 %</i></td>
        <td>Initial APR: <i>1257.1 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>417.72</i></td>
        <td>Final payment: <i>417.69</i></td>
        <td>Last scheduled payment day: <i>122</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>1,670.85</i></td>
        <td>Total principal: <i>1,000.00</i></td>
        <td>Total interest: <i>670.85</i></td>
    </tr>
</table>