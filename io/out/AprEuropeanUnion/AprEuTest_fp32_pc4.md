<h2>AprEuTest_fp32_pc4</h2>
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
        <td class="ci01" style="white-space: nowrap;">134.23</td>
        <td class="ci02">81.0155</td>
        <td class="ci03">81.01</td>
        <td class="ci04">53.22</td>
        <td class="ci05">0.00</td>
        <td class="ci06">264.04</td>
        <td class="ci07">81.0155</td>
        <td class="ci08">81.01</td>
        <td class="ci09">53.22</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">63</td>
        <td class="ci01" style="white-space: nowrap;">134.23</td>
        <td class="ci02">65.3182</td>
        <td class="ci03">65.31</td>
        <td class="ci04">68.92</td>
        <td class="ci05">0.00</td>
        <td class="ci06">195.12</td>
        <td class="ci07">146.3337</td>
        <td class="ci08">146.32</td>
        <td class="ci09">122.14</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">93</td>
        <td class="ci01" style="white-space: nowrap;">134.23</td>
        <td class="ci02">46.7117</td>
        <td class="ci03">46.71</td>
        <td class="ci04">87.52</td>
        <td class="ci05">0.00</td>
        <td class="ci06">107.60</td>
        <td class="ci07">193.0455</td>
        <td class="ci08">193.03</td>
        <td class="ci09">209.66</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">124</td>
        <td class="ci01" style="white-space: nowrap;">134.21</td>
        <td class="ci02">26.6181</td>
        <td class="ci03">26.61</td>
        <td class="ci04">107.60</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">219.6635</td>
        <td class="ci08">219.64</td>
        <td class="ci09">317.26</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>EU APR test amortisation schedule, first payment day 32, payment count 4</i></p>
<p>Generated: <i>2025-05-08 using library version 2.4.4</i></p>
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
                    <td>schedule length: <i><i>payment count</i> 4</i></td>
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
        <td>Initial cost-to-borrowing ratio: <i>69.23 %</i></td>
        <td>Initial APR: <i>1246.3 %</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>134.23</i></td>
        <td>Final payment: <i>134.21</i></td>
        <td>Last scheduled payment day: <i>124</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>536.90</i></td>
        <td>Total principal: <i>317.26</i></td>
        <td>Total interest: <i>219.64</i></td>
    </tr>
</table>