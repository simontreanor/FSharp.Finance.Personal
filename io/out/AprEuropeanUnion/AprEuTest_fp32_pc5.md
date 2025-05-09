<h2>AprEuTest_fp32_pc5</h2>
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
        <td class="ci06">317.26</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">32</td>
        <td class="ci01" style="white-space: nowrap;">117.73</td>
        <td class="ci02">81.0155</td>
        <td class="ci03">81.01</td>
        <td class="ci04">36.72</td>
        <td class="ci05">0.00</td>
        <td class="ci06">280.54</td>
        <td class="ci07">81.0155</td>
        <td class="ci08">81.01</td>
        <td class="ci09">36.72</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">63</td>
        <td class="ci01" style="white-space: nowrap;">117.73</td>
        <td class="ci02">69.4000</td>
        <td class="ci03">69.39</td>
        <td class="ci04">48.34</td>
        <td class="ci05">0.00</td>
        <td class="ci06">232.20</td>
        <td class="ci07">150.4155</td>
        <td class="ci08">150.40</td>
        <td class="ci09">85.06</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">93</td>
        <td class="ci01" style="white-space: nowrap;">117.73</td>
        <td class="ci02">55.5887</td>
        <td class="ci03">55.58</td>
        <td class="ci04">62.15</td>
        <td class="ci05">0.00</td>
        <td class="ci06">170.05</td>
        <td class="ci07">206.0042</td>
        <td class="ci08">205.98</td>
        <td class="ci09">147.21</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">124</td>
        <td class="ci01" style="white-space: nowrap;">117.73</td>
        <td class="ci02">42.0670</td>
        <td class="ci03">42.06</td>
        <td class="ci04">75.67</td>
        <td class="ci05">0.00</td>
        <td class="ci06">94.38</td>
        <td class="ci07">248.0711</td>
        <td class="ci08">248.04</td>
        <td class="ci09">222.88</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">155</td>
        <td class="ci01" style="white-space: nowrap;">117.72</td>
        <td class="ci02">23.3477</td>
        <td class="ci03">23.34</td>
        <td class="ci04">94.38</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">271.4189</td>
        <td class="ci08">271.38</td>
        <td class="ci09">317.26</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>EU APR test amortisation schedule, first payment day 32, payment count 5</i></p>
<p>Generated: <i>2025-05-09 using library version 2.4.5</i></p>
<h4>Basic Parameters</h4>
<table>
    <tr>
        <td>Evaluation Date</td>
        <td>2025-04-01</td>
    </tr>
    <tr>
        <td>Start Date</td>
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
                    <td>schedule length: <i><i>payment count</i> 5</i></td>
                </tr>
                <tr>
                    <td colspan="2" style="white-space: nowrap;">unit-period config: <i>monthly from 2025-05 on 03</i></td>
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
                    <td>method: <i>actuarial</i></td>
                </tr>
                <tr>
                    <td>rounding: <i>rounded down</i></td>
                    <td>APR method: <i>EU to 1 d.p.</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>85.54 %</i></td>
        <td>Initial APR: <i>1246.9 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>117.73</i></td>
        <td>Final payment: <i>117.72</i></td>
        <td>Last scheduled payment day: <i>155</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>588.64</i></td>
        <td>Total principal: <i>317.26</i></td>
        <td>Total interest: <i>271.38</i></td>
    </tr>
</table>