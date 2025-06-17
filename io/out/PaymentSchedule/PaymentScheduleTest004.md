<h2>PaymentScheduleTest004</h2>
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
        <td class="ci06">100.00</td>
        <td class="ci07">0.0000</td>
        <td class="ci08">0.00</td>
        <td class="ci09">0.00</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">10</td>
        <td class="ci01" style="white-space: nowrap;">36.48</td>
        <td class="ci02">8.0000</td>
        <td class="ci03">8.00</td>
        <td class="ci04">28.48</td>
        <td class="ci05">0.00</td>
        <td class="ci06">71.52</td>
        <td class="ci07">8.0000</td>
        <td class="ci08">8.00</td>
        <td class="ci09">28.48</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">41</td>
        <td class="ci01" style="white-space: nowrap;">36.48</td>
        <td class="ci02">17.7370</td>
        <td class="ci03">17.74</td>
        <td class="ci04">18.74</td>
        <td class="ci05">0.00</td>
        <td class="ci06">52.78</td>
        <td class="ci07">25.7370</td>
        <td class="ci08">25.74</td>
        <td class="ci09">47.22</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">72</td>
        <td class="ci01" style="white-space: nowrap;">36.48</td>
        <td class="ci02">13.0894</td>
        <td class="ci03">13.09</td>
        <td class="ci04">23.39</td>
        <td class="ci05">0.00</td>
        <td class="ci06">29.39</td>
        <td class="ci07">38.8264</td>
        <td class="ci08">38.83</td>
        <td class="ci09">70.61</td>
    </tr>
    <tr style="text-align: right;">
        <td class="ci00">102</td>
        <td class="ci01" style="white-space: nowrap;">36.44</td>
        <td class="ci02">7.0536</td>
        <td class="ci03">7.05</td>
        <td class="ci04">29.39</td>
        <td class="ci05">0.00</td>
        <td class="ci06">0.00</td>
        <td class="ci07">45.8800</td>
        <td class="ci08">45.88</td>
        <td class="ci09">100.00</td>
    </tr>
</table>
<h4>Description</h4>
<p><i>Payment count must not be exceeded</i></p>
<p>Generated: <i><a href="../GeneratedDate.html">see details</a></i></p>
<fieldset><legend>Basic Parameters</legend>
<table>
    <tr>
        <td>Evaluation Date</td>
        <td>2024-06-24</td>
    </tr>
    <tr>
        <td>Start Date</td>
        <td>2024-06-24</td>
    </tr>
    <tr>
        <td>Principal</td>
        <td>100.00</td>
    </tr>
    <tr>
        <td>Schedule options</td>
        <td>
            <fieldset>
                <legend>config: <i>auto-generate schedule</i></legend>
                <div>schedule length: <i><i>payment count</i> 4</i></div>
                <div>unit-period config: <i>monthly from 2024-07 on 04</i></div>
            </fieldset>
        </td>
    </tr>
    <tr>
        <td>Payment options</td>
        <td>
            <div>
                <div>rounding: <i>round using ToEven</i></div>
                <div>level-payment option: <i>lower&nbsp;final&nbsp;payment</i></div>
            </div>
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
            <div>
                <div>standard rate: <i>0.8 % per day</i></div>
                <div>method: <i>actuarial</i></div>
                <div>rounding: <i>round using ToEven</i></div>
                <div>APR method: <i>UK FCA</i></div>
                <div>APR precision: <i>1 d.p.</i></div>
                <div>cap: <i>total 100 %; daily 0.8 %</div>
            </div>
        </td>
    </tr>
</table></fieldset>
<fieldset><legend>Initial Stats</legend>
<div>
    <div>Initial interest balance: <i>0.00</i></div>
    <div>Initial cost-to-borrowing ratio: <i>45.88 %</i></div>
    <div>Initial APR: <i>1317.4 %</i></div>
    <div>Level payment: <i>36.48</i></div>
    <div>Final payment: <i>36.44</i></div>
    <div>Last scheduled payment day: <i>102</i></div>
    <div>Total scheduled payments: <i>145.88</i></div>
    <div>Total principal: <i>100.00</i></div>
    <div>Total interest: <i>45.88</i></div>
</div></fieldset>