# python-lucidmotors

Unofficial Python bindings to the Lucid Motors API. No affiliation with Lucid
Motors.

To install dependencies, assuming you have Python development tools already, run:

```
pip install -r requirements.txt
```

Try fiddling with `examples/vehicle_info.py`. With no arguments it will prompt
for your Lucid username and password, then print out your user profile and
vehicle information.

To generate a test case from your data, visit
[testmycode.cc](https://testmycode.cc). This will log in using your Lucid
account, remove identifying information from the API response, and let you
submit the anonymized data for review.

If you're feeling adventurous, try playing with `examples/test_all_actions.py` which
will run through every action we have figured out out thus far. A "stress test" of
sorts.
