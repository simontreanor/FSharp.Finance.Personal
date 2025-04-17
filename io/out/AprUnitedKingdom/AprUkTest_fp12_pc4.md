<h2>AprUkTest_fp12_pc4</h2>
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
        <td class="ci05">201.54</td>
        <td class="ci06">317.26</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">12</td>
        <td class="ci01" style="white-space: nowrap;">129.71</td>
        <td class="ci02">30.3808</td>
        <td class="ci03">129.71</td>
        <td class="ci04">0.00</td>
        <td class="ci05">71.83</td>
        <td class="ci06">317.26</td>
        <td class="ci07">30.3808</td>
        <td class="ci08">129.71</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">42</td>
        <td class="ci01" style="white-space: nowrap;">129.71</td>
        <td class="ci02">75.9520</td>
        <td class="ci03">71.83</td>
        <td class="ci04">57.88</td>
        <td class="ci05">0.00</td>
        <td class="ci06">259.38</td>
        <td class="ci07">106.3329</td>
        <td class="ci08">201.54</td>
        <td class="ci09">57.88</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">73</td>
        <td class="ci01" style="white-space: nowrap;">129.71</td>
        <td class="ci02">64.1654</td>
        <td class="ci03">0.00</td>
        <td class="ci04">129.71</td>
        <td class="ci05">0.00</td>
        <td class="ci06">129.67</td>
        <td class="ci07">170.4983</td>
        <td class="ci08">201.54</td>
        <td class="ci09">187.59</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">103</td>
        <td class="ci01" style="white-space: nowrap;">129.67</td>
        <td class="ci02">31.0430</td>
        <td class="ci03">0.00</td>
        <td class="ci04">129.67</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">201.5413</td>
        <td class="ci08">201.54</td>
        <td class="ci09">317.26</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>UK APR test amortisation schedule, first payment day 12, payment count 4</i></p>
<p>Generated: <i>2025-04-17 using library version 2.2.0</i></p>
<h4>Parameters</h4>
<table>
    <tr>
        <td>As-of</td>
        <td>2025-04-01</td>
    </tr>
    <tr>
        <td>Start</td>
        <td>2025-04-01</td>
    </tr>
    <tr>
        <td>Principal</td>
        <td>317.26</td>
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
                    <td colspan="2" style="white-space: nowrap;">unit-period config: <i>monthly from 2025-04 on 13</i></td>
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
                    <td colspan="2">cap: <i>total <i>n/a</i>; daily <i>n/a</i></td>
                </tr>
            </table>
        </td>
    </tr>
</table>
<h4>Initial Stats</h4>
<table>
    <tr>
        <td>Initial interest balance: <i>201.54</i></td>
        <td>Initial cost-to-borrowing ratio: <i>63.53 %</i></td>
        <td>Initial APR: <i>3033.9 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>129.71</i></td>
        <td>Final payment: <i>129.67</i></td>
        <td>Final scheduled payment day: <i>103</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>518.80</i></td>
        <td>Total principal: <i>317.26</i></td>
        <td>Total interest: <i>201.54</i></td>
    </tr>
</table>
